﻿using Microsoft.IdentityModel.Protocols.WSFederation.Metadata;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Thinktecture.IdentityModel.Tokens
{
    public class MetadataBasedIssuerNameRegistry : IssuerNameRegistry
    {
        private Uri _metadataAddress;
        private string _issuerName;

        private static ConfigurationBasedIssuerNameRegistry _registry;
        private static object _registryLock = new object();

        public MetadataBasedIssuerNameRegistry(Uri metadataAddress, string issuerName, bool lazyLoad = false)
        {
            _metadataAddress = metadataAddress;
            _issuerName = issuerName;

            if (!lazyLoad)
            {
                LoadMetadata();
            }
        }

        public override string GetIssuerName(SecurityToken securityToken)
        {
            if (_registry == null)
            {
                lock (_registryLock)
                {
                    if (_registry == null)
                    {
                        LoadMetadata();
                    }
                }
            }

            return _registry.GetIssuerName(securityToken);
        }

        protected virtual void LoadMetadata()
        {
            var client = new HttpClient { BaseAddress = _metadataAddress };
            var stream = client.GetStreamAsync("").Result;

            var serializer = new MetadataSerializer();
            var md = serializer.ReadMetadata(stream);

            var id = md.SigningCredentials.SigningKeyIdentifier;
            var clause = id.First() as X509RawDataKeyIdentifierClause;
            var cert = new X509Certificate2(clause.GetX509RawData());

            _registry = new ConfigurationBasedIssuerNameRegistry();
            _registry.AddTrustedIssuer(cert.Thumbprint, _issuerName);
        }
    }
}
