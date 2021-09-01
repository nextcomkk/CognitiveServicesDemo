namespace CognitiveServicesDemo
{
    public class Settings
    {
        // ConnectionStrings of Microsoft SQL database (loac or Azure) is in "appsetting.json".

        // Computer Vision API (Cognitive Services) key and endpoint
        public const string computervision_subscriptionKey = "";
        public const string computervision_endpoint = "";
        public const string computervision_language = "ja";

        // Cognitive Search API (Cognitive Services) admin key and indexs
        public const string cognitivesearch_adminApiKey = "";
        public const string cognitivesearch_endpoint = "";
        public const string cognitivesearch_index_SQL = "azuresql-usermedia-index";
        public const string cognitivesearch_index_Cosmos = "azurecosmos-usermedia-index";
        public const string cognitivesearch_index_Table = "azuretable-usermedia-index";
        public const string cognitivesearch_index_Blob = "azureblob-usermedia-index";

        // Azure Storage account
        public const string storage_accountName = "";
        public const string storage_accountKey = "";
        public const string storage_connectionString = "";

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
        public const string cosmos_endpointUri = "";
        public const string cosmos_accountKey = "";
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
