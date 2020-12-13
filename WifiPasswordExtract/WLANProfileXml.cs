namespace WifiPasswordExtract
{
    public class WLANProfileXml
    {
        // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.microsoft.com/networking/WLAN/profile/v1")]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.microsoft.com/networking/WLAN/profile/v1", IsNullable = false)]
        public partial class WLANProfile
        {
            private string nameField;

            private WLANProfileSSIDConfig sSIDConfigField;

            private string connectionTypeField;

            private string connectionModeField;

            private WLANProfileMSM mSMField;

            /// <remarks/>
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <remarks/>
            public WLANProfileSSIDConfig SSIDConfig
            {
                get
                {
                    return this.sSIDConfigField;
                }
                set
                {
                    this.sSIDConfigField = value;
                }
            }

            /// <remarks/>
            public string connectionType
            {
                get
                {
                    return this.connectionTypeField;
                }
                set
                {
                    this.connectionTypeField = value;
                }
            }

            /// <remarks/>
            public string connectionMode
            {
                get
                {
                    return this.connectionModeField;
                }
                set
                {
                    this.connectionModeField = value;
                }
            }

            /// <remarks/>
            public WLANProfileMSM MSM
            {
                get
                {
                    return this.mSMField;
                }
                set
                {
                    this.mSMField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.microsoft.com/networking/WLAN/profile/v1")]
        public partial class WLANProfileSSIDConfig
        {
            private WLANProfileSSIDConfigSSID sSIDField;

            private bool nonBroadcastField;

            /// <remarks/>
            public WLANProfileSSIDConfigSSID SSID
            {
                get
                {
                    return this.sSIDField;
                }
                set
                {
                    this.sSIDField = value;
                }
            }

            /// <remarks/>
            public bool nonBroadcast
            {
                get
                {
                    return this.nonBroadcastField;
                }
                set
                {
                    this.nonBroadcastField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.microsoft.com/networking/WLAN/profile/v1")]
        public partial class WLANProfileSSIDConfigSSID
        {
            private string hexField;

            private string nameField;

            /// <remarks/>
            public string hex
            {
                get
                {
                    return this.hexField;
                }
                set
                {
                    this.hexField = value;
                }
            }

            /// <remarks/>
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.microsoft.com/networking/WLAN/profile/v1")]
        public partial class WLANProfileMSM
        {
            private WLANProfileMSMSecurity securityField;

            /// <remarks/>
            public WLANProfileMSMSecurity security
            {
                get
                {
                    return this.securityField;
                }
                set
                {
                    this.securityField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.microsoft.com/networking/WLAN/profile/v1")]
        public partial class WLANProfileMSMSecurity
        {
            private WLANProfileMSMSecurityAuthEncryption authEncryptionField;

            private WLANProfileMSMSecuritySharedKey sharedKeyField;

            /// <remarks/>
            public WLANProfileMSMSecurityAuthEncryption authEncryption
            {
                get
                {
                    return this.authEncryptionField;
                }
                set
                {
                    this.authEncryptionField = value;
                }
            }

            /// <remarks/>
            public WLANProfileMSMSecuritySharedKey sharedKey
            {
                get
                {
                    return this.sharedKeyField;
                }
                set
                {
                    this.sharedKeyField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.microsoft.com/networking/WLAN/profile/v1")]
        public partial class WLANProfileMSMSecurityAuthEncryption
        {
            private string authenticationField;

            private string encryptionField;

            private bool useOneXField;

            /// <remarks/>
            public string authentication
            {
                get
                {
                    return this.authenticationField;
                }
                set
                {
                    this.authenticationField = value;
                }
            }

            /// <remarks/>
            public string encryption
            {
                get
                {
                    return this.encryptionField;
                }
                set
                {
                    this.encryptionField = value;
                }
            }

            /// <remarks/>
            public bool useOneX
            {
                get
                {
                    return this.useOneXField;
                }
                set
                {
                    this.useOneXField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.microsoft.com/networking/WLAN/profile/v1")]
        public partial class WLANProfileMSMSecuritySharedKey
        {
            private string keyTypeField;

            private bool protectedField;

            private string keyMaterialField;

            /// <remarks/>
            public string keyType
            {
                get
                {
                    return this.keyTypeField;
                }
                set
                {
                    this.keyTypeField = value;
                }
            }

            /// <remarks/>
            public bool @protected
            {
                get
                {
                    return this.protectedField;
                }
                set
                {
                    this.protectedField = value;
                }
            }

            /// <remarks/>
            public string keyMaterial
            {
                get
                {
                    return this.keyMaterialField;
                }
                set
                {
                    this.keyMaterialField = value;
                }
            }
        }
    }
}