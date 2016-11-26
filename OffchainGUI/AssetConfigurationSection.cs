using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffchainGUI
{
    public class AssetConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("Assets", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(AssetCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public AssetCollection Assets
        {
            get
            {
                return (AssetCollection)base["Assets"];
            }
        }
    }

    public class AssetCollection : ConfigurationElementCollection
    {
        public AssetCollection()
        {
            Console.WriteLine("AssetCollection Constructor");
        }

        public AssetConfig this[int index]
        {
            get { return (AssetConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(AssetConfig serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new AssetConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AssetConfig)element).AssetId;
        }

        public void Remove(AssetConfig serviceConfig)
        {
            BaseRemove(serviceConfig.AssetId);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }
    public class AssetConfig : ConfigurationElement
    {
        [ConfigurationProperty("assetId")]
        public string AssetId
        {
            get
            {
                return (string)this["assetId"];
            }
            set
            {
                this["assetId"] = value;
            }
        }

        [ConfigurationProperty("assetAddress")]
        public string AssetAddress
        {
            get
            {
                return (string)this["assetAddress"];
            }
            set
            {
                this["assetAddress"] = value;
            }
        }

        [ConfigurationProperty("name")]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("privateKey")]
        public string PrivateKey
        {
            get
            {
                return (string)this["privateKey"];
            }
            set
            {
                this["privateKey"] = value;
            }
        }

        [ConfigurationProperty("definitionUrl")]
        public string DefinitionUrl
        {
            get
            {
                return (string)this["definitionUrl"];
            }
            set
            {
                this["definitionUrl"] = value;
            }
        }

        [ConfigurationProperty("divisibility")]
        public int Divisibility
        {
            get
            {
                return (int)this["divisibility"];
            }
            set
            {
                this["divisibility"] = value;
            }
        }
    }
}
