using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace TestPlugin.DynamicHandlers.StaticDataHandlers;

public class SimpleStaticItemsDataHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new Dictionary<string, string>()
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { "key3", "value3" },
            { "key4", "value4" },
            { "key5", "value5" },
            { "key6", "value6" },
            { "key7", "value7" },
            { "key8", "value8" },
            { "key9", "value9" },
            { "key10", "value10" },
            { "key11", "value11" },
            { "key12", "value12" },
            { "key13", "value13" },
            { "key14", "value14 🐦" },
            { "key15", "value15 🐦" },
            { "key16", "value16 🐦" },
            { "key17", "value17 🐦" },
            { "key18", "value18 🐦" },
            { "key19", "value19 🐦" },
            { "key20", "value20 🐦" },
            { "key21", "value21 🐦" },
            { "key22", "value22 🐦" },
            { "key23", "value23 🐦" },
            { "key24", "value24 🐦" },
            { "key25", "value25 🐦" },
        }.Select(x => new DataSourceItem(x.Key, x.Value));
    }
}