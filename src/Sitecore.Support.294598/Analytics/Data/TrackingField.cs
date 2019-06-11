using Sitecore.Analytics;
using Sitecore.Analytics.Data;
using Sitecore.Analytics.Data.Items;
using Sitecore.Common;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Extensions.XElementExtensions;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Links;
using Sitecore.Marketing.Definitions;
using Sitecore.Marketing.Definitions.Campaigns;
using Sitecore.Marketing.Definitions.Events;
using Sitecore.Marketing.Definitions.Goals;
using Sitecore.Marketing.Definitions.PageEvents;
using Sitecore.Marketing.Definitions.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Sitecore.Support.Analytics.Data
{

    public class TrackingField : CustomField
    {
        private class MultipleProfileKeyCalculator : SimpleProfileKeyCalculator
        {
            public MultipleProfileKeyCalculator(ContentProfile profile)
              : base(profile)
            {
                Assert.ArgumentNotNull(profile, "profile");
            }

            public override void Calculate()
            {
                if (base.Profile.Presets != null && base.Profile.Presets.Count != 0)
                {
                    int num = 0;
                    Dictionary<string, double> dictionary = new Dictionary<string, double>();
                    foreach (string key2 in base.Profile.Presets.Keys)
                    {
                        if (string.IsNullOrEmpty(key2))
                        {
                            return;
                        }
                        ContentProfile presetProfileData = this.GetPresetProfileData(base.Profile, key2);
                        if (presetProfileData == null)
                        {
                            break;
                        }
                        num++;
                        ContentProfileKeyData[] keys = presetProfileData.Keys;
                        foreach (ContentProfileKeyData contentProfileKeyData in keys)
                        {
                            string text = contentProfileKeyData.Key.ToLowerInvariant();
                            if (!dictionary.ContainsKey(text))
                            {
                                dictionary.Add(text, 0.0);
                            }
                            Dictionary<string, double> dictionary2 = dictionary;
                            string key = text;
                            dictionary2[key] += contentProfileKeyData.Value;
                        }
                    }
                    if (num > 0)
                    {
                        List<string> list = new List<string>(dictionary.Keys);
                        List<string>.Enumerator enumerator2 = list.GetEnumerator();
                        try
                        {
                            while (enumerator2.MoveNext())
                            {
                                string current2 = enumerator2.Current;
                                Dictionary<string, double> dictionary2 = dictionary;
                                string key = current2;
                                dictionary2[key] /= (double)num;
                            }
                        }
                        finally
                        {
                            ((IDisposable)enumerator2).Dispose();
                        }
                        enumerator2 = list.GetEnumerator();
                        try
                        {
                            while (enumerator2.MoveNext())
                            {
                                string current3 = enumerator2.Current;
                                string keyId = current3;
                                ContentProfileKeyData contentProfileKeyData2 = base.Profile.Keys.FirstOrDefault((ContentProfileKeyData k) => string.Compare(k.Key, keyId, StringComparison.InvariantCultureIgnoreCase) == 0);
                                if (contentProfileKeyData2 != null)
                                {
                                    contentProfileKeyData2.Value = dictionary[keyId];
                                }
                            }
                        }
                        finally
                        {
                            ((IDisposable)enumerator2).Dispose();
                        }
                    }
                }
            }
        }

        private class MultipleWithPercentageProfileKeyCalculator : SimpleProfileKeyCalculator
        {
            public MultipleWithPercentageProfileKeyCalculator(ContentProfile profile)
              : base(profile)
            {
                Assert.ArgumentNotNull(profile, "profile");
            }

            public override void Calculate()
            {
                if (base.Profile.Presets != null && base.Profile.Presets.Count != 0)
                {
                    int num = 0;
                    Dictionary<string, double> dictionary = new Dictionary<string, double>();
                    foreach (string key2 in base.Profile.Presets.Keys)
                    {
                        if (string.IsNullOrEmpty(key2))
                        {
                            return;
                        }
                        ContentProfile presetProfileData = this.GetPresetProfileData(base.Profile, key2);
                        if (presetProfileData == null)
                        {
                            break;
                        }
                        num++;
                        double num2 = base.Profile.Presets[key2];
                        ContentProfileKeyData[] keys = presetProfileData.Keys;
                        foreach (ContentProfileKeyData contentProfileKeyData in keys)
                        {
                            string text = contentProfileKeyData.Key.ToLowerInvariant();
                            if (!dictionary.ContainsKey(text))
                            {
                                dictionary.Add(text, 0.0);
                            }
                            Dictionary<string, double> dictionary2 = dictionary;
                            string key = text;
                            dictionary2[key] += contentProfileKeyData.Value * num2 / 100.0;
                        }
                    }
                    if (num > 0)
                    {
                        foreach (string item in new List<string>(dictionary.Keys))
                        {
                            string keyId = item;
                            ContentProfileKeyData contentProfileKeyData2 = base.Profile.Keys.FirstOrDefault((ContentProfileKeyData k) => string.Compare(k.Key, keyId, StringComparison.InvariantCultureIgnoreCase) == 0);
                            if (contentProfileKeyData2 != null)
                            {
                                contentProfileKeyData2.Value = dictionary[keyId];
                            }
                        }
                    }
                }
            }
        }

        public class PageEventData
        {
            private readonly IMarketingDefinitions marketingDefinitions;

            public string Data
            {
                get;
                set;
            }

            public IEventDefinition Definition
            {
                get
                {
                    IEventDefinition eventDefinition = null;
                    if (this.PageEventDefinitionId != Guid.Empty)
                    {
                        IEventDefinition eventDefinition2 = this.marketingDefinitions.Goals[this.PageEventDefinitionId];
                        eventDefinition = (eventDefinition2 ?? this.marketingDefinitions.PageEvents[this.PageEventDefinitionId]);
                    }
                    if (eventDefinition == null)
                    {
                        object obj;
                        if (this.Name != null)
                        {
                            IEventDefinition eventDefinition2 = this.marketingDefinitions.Goals[this.Name];
                            obj = (eventDefinition2 ?? this.marketingDefinitions.PageEvents[this.Name]);
                        }
                        else
                        {
                            obj = null;
                        }
                        eventDefinition = (IEventDefinition)obj;
                    }
                    return eventDefinition;
                }
            }

            public string Name
            {
                get;
                set;
            }

            public Guid PageEventDefinitionId
            {
                get;
                set;
            }

            public PageEventData(IMarketingDefinitions marketingDefinitions)
              : this(marketingDefinitions, Guid.Empty)
            {
            }

            public PageEventData(IMarketingDefinitions marketingDefinitions, Guid pageEventDefinitionId)
            {
                Assert.ArgumentNotNull(marketingDefinitions, "marketingDefinitions");
                this.marketingDefinitions = marketingDefinitions;
                this.PageEventDefinitionId = pageEventDefinitionId;
            }
        }

        private class SimpleProfileKeyCalculator
        {
            private readonly ContentProfile profile;

            protected ContentProfile Profile
            {
                get
                {
                    return Assert.ResultNotNull(this.profile);
                }
            }

            public SimpleProfileKeyCalculator(ContentProfile profile)
            {
                Assert.ArgumentNotNull(profile, "profile");
                this.profile = profile;
            }

            public virtual void Calculate()
            {
                if (this.Profile.Presets != null && this.Profile.Presets.Count != 0)
                {
                    if (this.Profile.Presets.Count > 1)
                    {
                        string key = this.Profile.Presets.Keys.First();
                        this.profile.Presets = new Dictionary<string, double>
          {
            {
              key,
              100.0
            }
          };
                    }
                    string text = this.Profile.Presets.Keys.First();
                    if (!string.IsNullOrEmpty(text))
                    {
                        ContentProfile presetProfileData = this.GetPresetProfileData(this.Profile, text);
                        if (presetProfileData != null)
                        {
                            ContentProfileKeyData[] keys = presetProfileData.Keys;
                            foreach (ContentProfileKeyData contentProfileKeyData in keys)
                            {
                                string keyId = contentProfileKeyData.Key;
                                ContentProfileKeyData contentProfileKeyData2 = this.Profile.Keys.FirstOrDefault((ContentProfileKeyData k) => string.Compare(k.Key, keyId, StringComparison.InvariantCultureIgnoreCase) == 0);
                                if (contentProfileKeyData2 != null)
                                {
                                    contentProfileKeyData2.Value = contentProfileKeyData.Value;
                                }
                            }
                        }
                    }
                }
            }

            protected virtual ContentProfile GetPresetProfileData(ContentProfile profile, string presetKey)
            {
                Assert.ArgumentNotNull(profile, "profile");
                Assert.ArgumentNotNull(presetKey, "presetKey");
                TrackingField presetTrackingField = this.GetPresetTrackingField(profile, presetKey);
                if (presetTrackingField == null)
                {
                    return null;
                }
                return presetTrackingField.Profiles.FirstOrDefault((ContentProfile p) => p.ProfileID == profile.ProfileID);
            }

            protected virtual TrackingField GetPresetTrackingField(ContentProfile profile, string presetKey)
            {
                Assert.ArgumentNotNull(profile, "profile");
                Assert.ArgumentNotNull(presetKey, "presetKey");
                Item profileCardItem = profile.GetProfileCardItem(presetKey);
                if (profileCardItem == null)
                {
                    return null;
                }
                return this.GetTrackingField(profile, profileCardItem);
            }

            protected virtual TrackingField GetTrackingField(ContentProfile profile, Item presetItem)
            {
                Assert.ArgumentNotNull(profile, "profile");
                Assert.ArgumentNotNull(presetItem, "presetItem");
                Field field = presetItem.Fields[TrackingField.PresetFieldName];
                if (field == null)
                {
                    return null;
                }
                return new TrackingField(field);
            }
        }

        private const string presetFieldName = "Profile Card Value";

        private XDocument document;

        private List<ContentProfile> profiles = new List<ContentProfile>();

        public static string PresetFieldName
        {
            get
            {
                return Assert.ResultNotNull("Profile Card Value");
            }
        }

        public IEnumerable<string> CampaignIds
        {
            get
            {
                return from e in this.Root.Elements("campaign")
                       select e.Attribute("id") into a
                       where a != null
                       select a.Value;
            }
        }

        public IEnumerable<string> CampaignNames
        {
            get
            {
                return from e in this.Root.Elements("campaign")
                       select e.Attribute("title") into a
                       where a != null
                       select a.Value;
            }
        }

        public IEnumerable<ICampaignActivityDefinition> Campaigns
        {
            get
            {
                IMarketingDefinitions marketingDefinitions = Tracker.MarketingDefinitions;
                return from e in this.Root.Elements("campaign")
                       select e.Attribute("id") into a
                       where a != null
                       select marketingDefinitions.Campaigns[new ID(a.Value)];
            }
        }

        public IEnumerable<string> EventIds
        {
            get
            {
                return from e in this.Root.Elements("event")
                       select e.Attribute("id") into a
                       where a != null
                       select a.Value;
            }
        }

        public IEnumerable<string> EventNames
        {
            get
            {
                return from e in this.Root.Elements("event")
                       select e.Attribute("name") into a
                       where a != null
                       select a.Value;
            }
        }

        public IEnumerable<PageEventData> Events
        {
            get
            {
                IMarketingDefinitions marketingDefinitions = Tracker.MarketingDefinitions;
                return from e in this.Root.Elements("event")
                       select new PageEventData(marketingDefinitions, new Guid(e.GetAttributeValue("id")))
                       {
                           Name = e.GetAttributeValue("name"),
                           Data = e.GetAttributeValue("data")
                       };
            }
        }

        public bool Ignore
        {
            get
            {
                XAttribute xAttribute = this.Root.Attribute("ignore");
                if (xAttribute != null)
                {
                    return xAttribute.Value == "1";
                }
                return false;
            }
        }

        public ContentProfile[] Profiles
        {
            get
            {
                return Assert.ResultNotNull(this.profiles.ToArray());
            }
        }

        public XElement Root
        {
            get
            {
                return Assert.ResultNotNull(this.document.Element("tracking"));
            }
        }

        public bool IsEmpty
        {
            get
            {
                if (this.Root.Attributes().FirstOrDefault() == null)
                {
                    return this.Root.Elements().FirstOrDefault() == null;
                }
                return false;
            }
        }

        protected XDocument Xml
        {
            get
            {
                return this.document;
            }
        }

        public TrackingField(Field innerField)
          : base(innerField)
        {
            Assert.ArgumentNotNull(innerField, "innerField");
            this.Initialize();
        }

        public TrackingField(Field innerField, string runtimeValue)
          : base(innerField, runtimeValue)
        {
            Assert.ArgumentNotNull(innerField, "innerField");
            Assert.ArgumentNotNull(runtimeValue, "runtimeValue");
            this.Initialize();
        }

        internal static TrackingField FindTrackingField(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            TrackingField trackingField = TrackingField.GetTrackingField(item);
            if (trackingField != null)
            {
                return trackingField;
            }
            TemplateItem template = item.Template;
            if (template == null)
            {
                return null;
            }
            return TrackingField.GetTrackingField(template.InnerItem);
        }

        internal static bool HasTracking(Item item)
        {
            TrackingField trackingField = TrackingField.FindTrackingField(item);
            if (trackingField != null && !trackingField.IsEmpty)
            {
                return true;
            }
            return false;
        }

        public void AcceptChanges()
        {
            base.Value = this.GetFieldValue();
        }

        public string GetFieldValue()
        {
            this.InitializeDocument();
            XDocument xDocument = this.document;
            XElement xElement = xDocument.Element("tracking");
            if (xElement == null)
            {
                xElement = new XElement("tracking");
                xDocument.Add(xElement);
            }
            List<XElement> list = xElement.Elements("profile").ToList();
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Remove();
            }
            foreach (XElement item in from profile in this.Profiles
                                      where profile.SaveToField
                                      select profile.ToXElement())
            {
                xElement.Add(item);
            }
            return xDocument.ToString();
        }

        public ContentProfile GetProfile(Item profileItem)
        {
            Assert.ArgumentNotNull(profileItem, "profileItem");
            return this.Profiles.FirstOrDefault((ContentProfile profile) => profile.ProfileID == profileItem.ID);
        }

        public bool HasCampaign(string title)
        {
            return (from e in this.Root.Elements("campaign")
                    select e.Attribute("title")).Any(delegate (XAttribute a)
                    {
                        if (a != null)
                        {
                            return string.Compare(a.Value, title, StringComparison.InvariantCultureIgnoreCase) == 0;
                        }
                        return false;
                    });
        }

        public bool HasCampaign(Guid id)
        {
            return (from e in this.Root.Elements("campaign")
                    select e.Attribute("id")).Any(delegate (XAttribute a)
                    {
                        if (a != null)
                        {
                            return new Guid(a.Value) == id;
                        }
                        return false;
                    });
        }

        public bool HasEvent(string name)
        {
            Assert.ArgumentNotNull(name, "name");
            return (from e in this.Root.Elements("event")
                    select e.Attribute("name")).Any(delegate (XAttribute a)
                    {
                        if (a != null)
                        {
                            return string.Compare(a.Value, name, StringComparison.InvariantCultureIgnoreCase) == 0;
                        }
                        return false;
                    });
        }

        public bool HasGoal(string name)
        {
            Assert.ArgumentNotNull(name, "name");
            return this.HasEvent(name);
        }

        public static void UpdateKeyValues(ContentProfile profile)
        {
            Assert.ArgumentNotNull(profile, "profile");
            Assert.IsFalse(profile.ProfileID == ID.Null, "profile ID");
            if (profile.Presets != null && profile.Presets.Count != 0)
            {
                Item profileItem = profile.GetProfileItem();
                if (profileItem != null)
                {
                    Item presetsFolder = ProfileUtil.GetPresetsFolder(profileItem);
                    if (presetsFolder != null)
                    {
                        SimpleProfileKeyCalculator simpleProfileKeyCalculator = null;
                        string strA = ((BaseItem)presetsFolder)["Authoring Selection"];
                        if (string.Compare(strA, "{C4960DD5-8B07-4025-8E48-57C3BC578CE1}", StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            simpleProfileKeyCalculator = new SimpleProfileKeyCalculator(profile);
                        }
                        else if (string.Compare(strA, "{DF9486E3-C239-406E-83DD-7A30BEF2599D}", StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            simpleProfileKeyCalculator = new MultipleProfileKeyCalculator(profile);
                        }
                        else if (string.Compare(strA, "{2DE135D7-FB39-42B2-B10A-13CB4285E5C5}", StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            simpleProfileKeyCalculator = new MultipleWithPercentageProfileKeyCalculator(profile);
                        }
                        if (simpleProfileKeyCalculator != null)
                        {
                            simpleProfileKeyCalculator.Calculate();
                        }
                    }
                }
            }
        }

        internal static TrackingField GetTrackingField(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            Field field = item.Fields["__Tracking"];
            if (field != null)
            {
                return new TrackingField(field);
            }
            return null;
        }

        private XElement Find(string elementName, ID id, string name)
        {
            XElement result = null;
            foreach (XElement item in this.Root.Elements(elementName))
            {
                XAttribute xAttribute = item.Attribute("id");
                if (xAttribute != null && xAttribute.Value == id.ToString())
                {
                    return item;
                }
                XAttribute xAttribute2 = item.Attribute("name") ?? item.Attribute("title");
                if (xAttribute2 != null && xAttribute2.Value == name)
                {
                    result = item;
                }
            }
            return result;
        }

        public override void Relink(ItemLink link, Item newLink)
        {
            Item targetItem = link.GetTargetItem();
            if (targetItem != null)
            {
                ID templateID = targetItem.TemplateID;
                if (templateID != newLink.TemplateID)
                {
                    throw new Exception(Translate.Text("Cannot relink to an item with a different template."));
                }
                string elementType = this.GetElementType(templateID);
                if (elementType != null)
                {
                    XElement xElement = this.Find(elementType, targetItem.ID, targetItem.Name);
                    if (xElement != null)
                    {
                        xElement.SetAttributeValue("id", newLink.ID.ToString());
                        xElement.SetAttributeValue((elementType == "campaign") ? "title" : "name", newLink.Name);
                    }
                    this.AcceptChanges();
                }
            }
        }

        private string GetElementType(ID templateID)
        {
            string result = null;
            if (templateID.Guid == Sitecore.Marketing.Definitions.Campaigns.WellKnownIdentifiers.CampaignActivityDefinitionTemplateId)
            {
                result = "campaign";
            }
            if (templateID.Guid == Sitecore.Marketing.Definitions.Goals.WellKnownIdentifiers.GoalDefinitionTemplateId || templateID.Guid == Sitecore.Marketing.Definitions.PageEvents.WellKnownIdentifiers.PageEventDefinitionTemplateId)
            {
                result = "event";
            }
            if (templateID == ProfileItem.TemplateID)
            {
                result = "profile";
            }
            return result;
        }

        protected internal virtual void UpdateLink(Item item, ItemChanges changes)
        {
            PropertyChange propertyChange = default(PropertyChange);
            if (changes.Properties.TryGetValue("name", out propertyChange))
            {
                string text = propertyChange.OriginalValue as string;
                string value = propertyChange.Value as string;
                if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(value))
                {
                    string elementType = this.GetElementType(item.TemplateID);
                    if (elementType != null)
                    {
                        XElement xElement = this.Find(elementType, item.ID, text);
                        if (xElement != null)
                        {
                            xElement.SetAttributeValue("id", item.ID);
                            xElement.SetAttributeValue((elementType == "campaign") ? "title" : "name", value);
                        }
                        this.AcceptChanges();
                    }
                }
            }
        }

        public override void RemoveLink(ItemLink itemLink)
        {
            Item targetItem = itemLink.GetTargetItem();
            if (targetItem != null)
            {
                string elementType = this.GetElementType(targetItem.TemplateID);
                if (elementType != null)
                {
                    XElement xElement = this.Find(elementType, targetItem.ID, targetItem.Name);
                    if (xElement != null)
                    {
                        xElement.Remove();
                        this.AcceptChanges();
                    }
                }
            }
        }

        public override void ValidateLinks(LinksValidationResult result)
        {
            foreach (string campaignId in this.CampaignIds)
            {
                Item item = null;
                ID itemId = default(ID);
                if (ID.TryParse(campaignId, out itemId))
                {
                    item = base.InnerField.Database.GetItem(itemId);
                }
                if (item == null || !TemplateManager.GetTemplate(item).InheritsFrom(Sitecore.Marketing.Definitions.Campaigns.WellKnownIdentifiers.CampaignActivityDefinitionTemplateId.ToID()))
                {
                    result.AddBrokenLink(campaignId);
                }
                else
                {
                    result.AddValidLink(item, item.Paths.Path);
                }
            }
            foreach (PageEventData @event in this.Events)
            {
                IEventDefinition eventDefinition = null;
                if (@event.PageEventDefinitionId != Guid.Empty)
                {
                    IEventDefinition eventDefinition2 = Tracker.MarketingDefinitions.Goals[@event.PageEventDefinitionId];
                    eventDefinition = (eventDefinition2 ?? Tracker.MarketingDefinitions.PageEvents[@event.PageEventDefinitionId]);
                }
                if (eventDefinition == null && @event.Name != null)
                {
                    IEventDefinition eventDefinition2 = Tracker.MarketingDefinitions.Goals[@event.Name];
                    eventDefinition = (eventDefinition2 ?? Tracker.MarketingDefinitions.PageEvents[@event.Name]);
                }
                if (eventDefinition == null)
                {
                    Item item2 = base.InnerField.Database.GetItem(Sitecore.Marketing.Definitions.Goals.WellKnownIdentifiers.MarketingCenterGoalsContainerId.ToID());
                    Assert.IsNotNull(item2, string.Format("Goals item with ID {0} was not found.", Sitecore.Marketing.Definitions.Goals.WellKnownIdentifiers.MarketingCenterGoalsContainerId));
                    result.AddBrokenLink(FileUtil.MakePath(item2.Paths.Path, @event.Name, '/'));
                }
                else
                {
                    Item item3 = base.InnerField.Database.GetItem(eventDefinition.Id.ToID());
                    result.AddValidLink(item3, item3.Paths.Path);
                }
            }
            foreach (ContentProfile item4 in from p in this.Profiles
                                             where p.SaveToField
                                             select p)
            {
                Item profileItem = item4.GetProfileItem();
                result.AddValidLink(profileItem, profileItem.Paths.Path);
            }
        }

        [Obsolete("This method is obsolete and will be removed in a future version.")]
        protected static void ProcessProfileKeys(ContentProfile profile, IProfileDefinition profileDefinition)
        {
            TrackingField.DoProcessProfileKeys(profile, profileDefinition);
        }

        internal static void DoProcessProfileKeys(ContentProfile profile, IProfileDefinition profileDefinition)
        {
            Assert.ArgumentNotNull(profile, "profile");
            Assert.ArgumentNotNull(profileDefinition, "profileDefinition");
            Assert.IsFalse(profile.ProfileID == ID.Null, "profile ID");
            foreach (IProfileKeyDefinition key in profileDefinition.Keys)
            {
                ContentProfileKeyData contentProfileKeyData = profile.Keys.FirstOrDefault((ContentProfileKeyData k) => string.Compare(k.Key, key.Alias, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (contentProfileKeyData == null)
                {
                    contentProfileKeyData = new ContentProfileKeyData(key)
                    {
                        Value = key.GetDefaultValue()
                    };
                    profile.AddKey(contentProfileKeyData);
                }
            }
            foreach (ContentProfileKeyData item in from key in profile.Keys
                                                   where key.IsEmpty
                                                   select key)
            {
                profile.RemoveKey(item);
            }
            TrackingField.UpdateKeyValues(profile);
        }

        private static void ProcessProfileKeys(ContentProfile profile, ContentProfile profileWithDefaultValues)
        {
            Assert.ArgumentNotNull(profile, "profile");
            Assert.ArgumentNotNull(profileWithDefaultValues, "profileWithDefaultValues");
            Assert.IsFalse(profile.ProfileID == ID.Null, "profile ID");
            ContentProfileKeyData[] keys = profileWithDefaultValues.Keys;
            foreach (ContentProfileKeyData keyItem in keys)
            {
                if (profile.Keys.FirstOrDefault((ContentProfileKeyData k) => string.Compare(k.Key, keyItem.Name, StringComparison.InvariantCultureIgnoreCase) == 0) == null)
                {
                    profile.AddKey(keyItem);
                }
            }
            foreach (ContentProfileKeyData item in from key in profile.Keys
                                                   where key.IsEmpty
                                                   select key)
            {
                profile.RemoveKey(item);
            }
            TrackingField.UpdateKeyValues(profile);
        }

        protected void Initialize()
        {
            this.InitializeDocument();
            this.InitializeProfiles();
        }

        protected void InitializeDocument()
        {
            if (this.document == null)
            {
                string value = base.Value;
                if (string.IsNullOrEmpty(value))
                {
                    this.document = new XDocument(new XElement("tracking"));
                }
                else
                {
                    this.document = XDocument.Parse(value);
                }
            }
        }

        protected void InitializeProfiles()
        {
            MarketingDefinitions marketingDefinitions = new MarketingDefinitions(base.InnerField.Language, ServiceLocator.ServiceProvider.GetDefinitionManagerFactory());
            
            System.Reflection.PropertyInfo pi = marketingDefinitions.GetType().GetProperty("ProfilesWithDefaultValues", System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            this.profiles = ((List<ContentProfile>)pi.GetValue(marketingDefinitions)).ToList();//marketingDefinitions.ProfilesWithDefaultValues.ToList();
            if (!string.IsNullOrEmpty(base.Value))
            {
                foreach (XElement item in this.Root.Elements("profile"))
                {
                    ContentProfile contentProfile = ContentProfile.Parse(item, base.InnerField.Item, marketingDefinitions);
                    if (contentProfile != null)
                    {
                        contentProfile.IsSavedInField = true;
                        ContentProfile contentProfile2 = this.ReplaceItem(contentProfile);
                        if (contentProfile2 != null)
                        {
                            TrackingField.ProcessProfileKeys(contentProfile, contentProfile2);
                        }
                    }
                }
            }          
        }

        [Obsolete("This method is obsolete and will be removed in a future version.")]
        protected void NormalizeProfiles(IMarketingDefinitions marketingDefinitions)
        {
            Assert.ArgumentNotNull(marketingDefinitions, "marketingDefinitions");
            foreach (IProfileDefinition profile in marketingDefinitions.Profiles)
            {
                ContentProfile contentProfile = this.profiles.FirstOrDefault((ContentProfile p) => string.Compare(p.Key, profile.Alias, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (contentProfile == null)
                {
                    contentProfile = new ContentProfile(profile)
                    {
                        IsSavedInField = false
                    };
                    this.profiles.Add(contentProfile);
                }
                TrackingField.ProcessProfileKeys(contentProfile, profile);
            }
        }

        private ContentProfile ReplaceItem(ContentProfile profile)
        {
            for (int i = 0; i < this.profiles.Count; i++)
            {
                if (string.Compare(profile.Key, this.profiles[i].Key, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    ContentProfile result = this.profiles[i];
                    this.profiles[i] = profile;
                    return result;
                }
            }
            return null;
        }
    }

}