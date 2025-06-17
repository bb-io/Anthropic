using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using TestPlugin.Dtos.TypesActions;

namespace TestPlugin.Dtos.NullActions
{
    public class ReturnNullTypesResponse
    {
        public ReturnNullTypesResponse()
        {
            NullString = null;
            NullNumber = null;
            NullBoolean = null;
            NullDate = null;
            NullFile = null;
            NullArray = null;
        }

        [Display("Null String")]
        public string NullString { get; set; }

        [Display("Null Number")]
        public int? NullNumber { get; set; }

        [Display("Null Boolean")]
        public bool? NullBoolean { get; set; }

        [Display("Null Date")]
        public DateTime? NullDate { get; set; }

        [Display("Null File")]
        public FileReference NullFile { get; set; }

        [Display("Null Array")]
        public IEnumerable<ArrayItem> NullArray { get; set; }
    }
}
