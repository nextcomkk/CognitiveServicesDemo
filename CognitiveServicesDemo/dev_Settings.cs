namespace CognitiveServicesDemo
{
    public class dev_Settings
    {
        // ConnectionStrings of Microsoft SQL database (loac or Azure) is in "appsetting.json".

        // Computer Vision API (Cognitive Services) key and endpoint
        public const string computervision_subscriptionKey = "56351b0970694b51be73761704752e78";
        public const string computervision_endpoint = "https://nativesense-test.cognitiveservices.azure.com/";
        public const string computervision_language = "ja";

        // Cognitive Search API (Cognitive Services) admin key and indexs
        public const string cognitivesearch_adminApiKey = "6AD872ADFC6E014336424204D698127F";
        public const string cognitivesearch_endpoint = "https://cspa-cognitivesearch.search.windows.net";
        public const string cognitivesearch_index_SQL = "azuresql-usermedia-index";
        public const string cognitivesearch_index_Cosmos = "azurecosmos-usermedia-index";
        public const string cognitivesearch_index_Table = "azuretable-usermedia-index";
        public const string cognitivesearch_index_Blob = "azureblob-usermedia-index";

        // Azure Storage account
        public const string storage_accountName = "cspastorage1";
        public const string storage_accountKey = "V33ZcXYoEziBBuhFB9CyKtyzQSCPqR0jLbCct22MrisesNOoYOvEBownppWLrw/bNvWUjcqpsKX7p4b+Bio3Mg==";
        public const string storage_connectionString = "DefaultEndpointsProtocol=https;AccountName=cspastorage1;AccountKey=V33ZcXYoEziBBuhFB9CyKtyzQSCPqR0jLbCct22MrisesNOoYOvEBownppWLrw/bNvWUjcqpsKX7p4b+Bio3Mg==;EndpointSuffix=core.windows.net";

        // Azure Storage Blob container for storing images.
        public const string blob_containerName_image = "imagecontainer";

        /* You can choose where to save the image info (metadata and computer vision analysis tag results) as follows
         *
         *  1) SQL database (Azure or local) ... as SQL Record  (SQL structured)
         *  2) Azure Cosmos DB               ... as Items       (JSON)
         *  3) Azure Storage Table           ... as Entity      (NoSQL structured)
         *  4) Azure Storage Blob            ... as File        (JSON)
         */

        // 1) SQLdatabase
        //    Not need to configure it here (configure in appsetting.json).

        // 2) Azure Cosmos DB
        public const string cosmos_endpointUri = "https://cspa-cosmos.documents.azure.com:443/";
        public const string cosmos_accountKey = "Sj73OxoJPoJjLzOjsMOZ4JMw8AR1Oo1C3A27vkErL0AibqOP006Wxg1IdU2VMoNzObpBQEAHKc5fxWaCsRPYRw==";
        public const string cosmos_databaseName = "cspacosmos";
        public const string cosmos_containerName = "UserMedia";

        // 3) Azure Storage Table 
        public const string storage_tableName = "UserMedia";

        // 4) Azure Storage Blob 
        public const string blob_containerName_json = "jsoncontainer";

        // Other Settings
        public const double tag_confidence_threshold = 0.80;        // Tag confidence threshold for search
        public const int displayMaxItems_search = 250;
        public const int pageSize_manage = 100;
        public const int pageSize_regist = 50;
    }
}
