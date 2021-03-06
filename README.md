# Certes ACME Client

[![Travis](https://travis-ci.org/fszlin/certes.svg)](https://travis-ci.org/fszlin/certes)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/4wwiivqs8rl0l63q?svg=true)](https://ci.appveyor.com/project/fszlin/certes)
[![NuGet](https://buildstats.info/nuget/certes)](https://www.nuget.org/packages/certes/)
[![MyGet](https://buildstats.info/myget/dymetis/certes?includePreReleases=true)](https://www.myget.org/feed/dymetis/package/nuget/certes)

Certes is a client implantation for the Automated Certificate Management
Environment (ACME) protocol, build on .NET Core. It is aimed to provide a easy
to use API for managing certificates using scripts during build process.

Before [Let's Encrypt](https://letsencrypt.org), SSL/TLS certificate for HTTPS
was a privilege for who can afford it. With Certes, you can quickly generate
certificates using .NET or command line, and it is **free**.

# Get Certified in 5 Minutes

Install [.NET Core](https://www.microsoft.com/net/core)

Donwload the [latest release](https://github.com/fszlin/certes/releases), 
   and extract the files

Run these commands to start the authorization process

```Bash
    # Create new registration on LE, and accept terms of services
    certes register --email your_email@my_domain.com --agree-tos

    # Initialize authorization for host name(s)
    certes authz --v my_domain.com #--v www.my_domain.com --v my_domain2.com

    # Show the http-01 key authorization for specified host name(s)
    certes authz --key-authz http-01 --v my_domain.com #--v www.my_domain.com --v my_domain2.com
```

Make changes to your site so that it serves the **key authorization string** 
   on the well know path.
  * The **key authorization string** consists of the token and the thumbprint
    of the registration key, in form of `<token>.<thumbprint>`
  * You can simply save the **key authorization string** in a text file, and
    upload it to `http://my_domain.com/.well-known/acme-challenge/<token>`
  * For testing purpuse, if you are hosting an ASP.NET Core app, you can add
    the following to ```Configure``` method of ```Startup``` class

```C#
        app.Map("/.well-known/acme-challenge", sub =>
        {
            sub.Run(async context =>
            {
                var path = context.Request.Path.ToUriComponent();
                if (path?.Length > 1 && path.StartsWith("/"))
                {
                    context.Response.ContentType = "plain/text";
                    await context.Response.WriteAsync($"{path.Substring(1)}.<thumbprint>");
                }
            });
        });
```

  * For more details, see [section 6.4.1 of the ACME spec](https://tools.ietf.org/html/draft-ietf-acme-acme-02#section-6.4.1)

Continue the authorization process and generate the certificate

```Bash
    # Complete the http-01 challenge
    certes authz --complete-authz http-01 --v my_domain.com #--v www.my_domain.com --v my_domain2.com

    # Check the challenge status, wait until it becomes "valid"
    certes authz --refresh http-01 --v my_domain.com #--v www.my_domain.com --v my_domain2.com

    # Create a certificate with the distinguished name, and additional SAN names
    certes cert --name mycert --distinguished-name "CN=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN=my_domain.com" #--v www.my_domain.com --v my_domain2.com

    # Export the certificate in DER
    certes cert --name mycert --export-cer ./mycert.cer

    # Export the certificate's private key in PEM
    certes cert --name mycert --export-key ./mycert.key

    # Export the certificate with private key in PFX
    certes cert --name mycert --export-pfx ./mycert.pfx --password abcd1234

    # Revoke the certificate
    certes cert --name mycert --revoke
```

Install the certificate on your host server.

More...
  * Append ```--server https://acme-staging.api.letsencrypt.org/directory``` to the commands
    for testing again LE staging server.
  * By default, the account and contextual data are saved in **data.json**, 
    use ```--path``` option to change the location.

# Get Started

You can get Certes by grabbing the latest
[NuGet package](https://www.nuget.org/packages/Certes).
