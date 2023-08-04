using FellowOakDicom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace boDicom.WPF
{
    public class DicomSequenceItem
    {
        public int Index { get; set; }
        public string Name { get; set; } = "Item";
        public List<DicomTagInfo> Items { get; set; }
        public DicomSequenceItem()
        {
            Items = new List<DicomTagInfo>();
        }
        public DicomSequenceItem(int index, DicomDataset dicomDataset)
        {
            Index = index;
            Items = new List<DicomTagInfo>();
            foreach(DicomItem item in dicomDataset)
            {
                if (item is DicomElement)
                {
                    Items.Add(new DicomTagInfo(item as DicomElement));
                }
                else if (item is DicomSequence)
                {
                    Items.Add(new DicomTagInfo(item as DicomSequence));
                }
            }
        }
    }
    public class DicomTagInfo
    {
        public string Tag { get; set; }
        public string VR { get; set; }
        public string TagName { get; set; }
        public string Value { get; set; }
        public List<DicomSequenceItem> SequenceItem { get; set; }
        public int SequenceItemCount { get { return SequenceItem.Count; } }
        public DicomTagInfo() 
        {
            SequenceItem = new List<DicomSequenceItem>();
        }
        public DicomTagInfo(DicomTag tag, DicomVR vr, string value)
        {
            Tag = tag.ToString();
            VR = vr.Code;
            TagName = tag.DictionaryEntry.Name;
            Value = value;
        }
        public DicomTagInfo(DicomElement elem)
        {
            Tag = elem.Tag.ToString();
            VR = elem.ValueRepresentation.Code;
            TagName = elem.Tag.DictionaryEntry.Name;
            Value = elem.Get<string>();
            SequenceItem = new List<DicomSequenceItem>();
        }
        public DicomTagInfo(DicomSequence sqElem)
        {
            Tag = sqElem.Tag.ToString();
            VR = sqElem.ValueRepresentation.ToString();
            TagName = "Sequnce Item";
            Value = "";
            SequenceItem = new List<DicomSequenceItem>();
            int index = 0;
            foreach (DicomDataset sqItem in sqElem)
            {
                SequenceItem.Add(new DicomSequenceItem(index, sqItem));
                index++;
            }
        }
    }
}
