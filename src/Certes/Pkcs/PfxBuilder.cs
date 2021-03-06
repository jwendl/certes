﻿using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Certes.Pkcs
{
    public class PfxBuilder
    {
        private static X509Certificate[] embeddedIssuers;
        private readonly X509Certificate certificate;
        private readonly KeyInfo privateKeyInfo;
        private readonly Dictionary<X509Name, X509Certificate> issuers = EmbeddedIssuers.ToDictionary(c => c.SubjectDN, c => c);
        private readonly X509CertificateParser certParser = new X509CertificateParser();

        public bool FullChain { get; set; } = true;

        private static X509Certificate[] EmbeddedIssuers
        {
            get
            {
                if (embeddedIssuers == null)
                {
                    var certParser = new X509CertificateParser();
                    var assembly = typeof(PfxBuilder).GetTypeInfo().Assembly;
                    embeddedIssuers = assembly
                        .GetManifestResourceNames()
                        .Where(n => n.EndsWith(".cer"))
                        .Select(n =>
                        {
                            using (var stream = assembly.GetManifestResourceStream(n))
                            {
                                return certParser.ReadCertificate(stream);
                            }
                        })
                        .ToArray();
                }

                return embeddedIssuers;
            }
        }

        public PfxBuilder(byte[] certificate, KeyInfo privateKeyInfo)
        {
            this.certificate = certParser.ReadCertificate(certificate);
            this.privateKeyInfo = privateKeyInfo;
        }

        public void AddIssuer(byte[] certificate)
        {
            var cert = certParser.ReadCertificate(certificate);
            this.issuers[cert.SubjectDN] = cert;
        }

        public byte[] Build(string friendlyName, string password)
        {
            var keyPair = privateKeyInfo.CreateKeyPair();
            var store = new Pkcs12StoreBuilder().Build();

            var entry = new X509CertificateEntry(certificate);
            store.SetCertificateEntry(friendlyName, entry);

            if (FullChain)
            {
                var certChain = FindIssuers();
                var certChainEntries = certChain.Select(c => new X509CertificateEntry(c)).ToList();
                certChainEntries.Add(entry);
                
                store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(keyPair.Private), certChainEntries.ToArray());
            }
            else
            {
                store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(keyPair.Private), new[] { entry });
            }

            using (var buffer = new MemoryStream())
            {
                store.Save(buffer, password.ToCharArray(), new SecureRandom());
                return buffer.ToArray();
            }
        }

        private IList<X509Certificate> FindIssuers()
        {
            var certificates = issuers.Values
                .Select(cert => new
                {
                    IsRoot = cert.IssuerDN.Equivalent(cert.SubjectDN),
                    Cert = cert
                });

            var rootCerts = new HashSet(certificates.Where(c => c.IsRoot).Select(c => new TrustAnchor(c.Cert, null)));
            var intermediateCerts = certificates.Where(c => !c.IsRoot).Select(c => c.Cert).ToList();
            intermediateCerts.Add(certificate);

            var target = new X509CertStoreSelector()
            {
                Certificate = certificate
            };

            var builderParams = new PkixBuilderParameters(rootCerts, target)
            {
                IsRevocationEnabled = false
            };

            builderParams.AddStore(
                X509StoreFactory.Create(
                    "Certificate/Collection",
                    new X509CollectionStoreParameters(intermediateCerts)));

            var builder = new PkixCertPathBuilder();
            var result = builder.Build(builderParams);
            
            var fullChain = result.CertPath.Certificates.Cast<X509Certificate>().ToArray();
            return fullChain;
        }
    }
}
