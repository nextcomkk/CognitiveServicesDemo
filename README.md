# CognitveServicesDemo

- *このプロジェクトは、ASP .NET Core MVC Web Application (.NET 5.0) で作成されています*
- *Azure SQL database、CosmosDB、Storage Table、Storage Blob を利用しています*

## 環境構築手順

### 1. **必要**
- Visual Studio 2019 (ローカル環境)
- Gitクライアントツール(ローカル環境)
- Microsoft Azure アカウント

### 2. **リポジトリ取得**
`git clone https://github.com/nextcomkk/CognitveServicesDemo`

### 3. **パッケージインストール**

- *プロジェクトを Visual Studioで開き、「GuGetパッケージの管理」より下記パッケージをインストールします*

> Azure.Data.Tables  
> Azure.Search.Documentes  
> Microsoft.AspNet.WebApi.Core  
> Microsoft.AspNeCoretDiagnostics.EntityFrameworkCore  
> Microsoft.AspNeCore.Identity.EntityFrameworkCore  
> Microsoft.AspNeCore.Identity.UI  
> Microsoft.Azure.CognitiveSerivces.Vision.ComputerVision  
> Microsoft.Azure.Cosmos  
> Microsoft.EntityFrameworkCore.SqlServer  
> Microsoft.EntityFrameworkCore.Tools  
> Microsoft.Extentions.Logging.AzureAppServices  
> WindowsAzure.Storage  


### 4. **Azure リソース作成と設定**


*Azure ポータル (https://portal.azure.com) にて、各種リソースの作成と設定を行います*

#### 　4-1) Computer Vision リソース作成


#### 　4-2) Cognitive Search リソース作成


#### 　4-3) SQL server 及び SQL database リソース作成

　　*開発環境から接続するには、下記設定が必要です (SQL Server)*  
  
　　`ファイアウォールと仮想ネットワーク → クライアントIPの追加 → Azure サービスおよびリソースにこのサーバーへのアクセスを許可する`

#### 　4-4) Cosmos DB アカウント作成
　　※ContainerとTableは手動で作成する必要はありません

#### 　4-5) Storage アカウント作成
　　※ContainerとTableは手動で作成する必要はありません

### 5. **appsettings.json 設定**
　　*Visual Studio ソリューションエクスプローラーより appsettings.json を開いて下記を設定します*

- ConnectionStrings > DefaultConnection  
	- Azure SQL server ( 4-3 で作成 ) の接続文字列を設定  
	- (開発時はローカルで作成したSQL serverでも可)  

### 6. **Setting.cs 設定**
　　*Visual Studio ソリューションエクスプローラーより Setting.cs を開いて下記を設定します*  

- **Computer Vision API (Cognitive Services)　…「キーとエンドポイント」より転記 ( 4-1で作成 )**  
	- computervision_subscriptionKey　…キーを転記
	- computervision_endpoint　…エンドポイントを転記

- **Cognitive Search API (Cognitive Services)　…「概要＆キー」より転記 ( 4-2で作成 )**  
	- cognitivesearch_AdminKey  …キーを転記
	- cognitivesearch_endpoint  …概要のURLを転記

- **Azure Storage account　…「アクセス キー」より転記 ( 4-5で作成 )**  
	- storage_accountName　…ストレージ アカウント名
	- storage_accountKey　…key1のキー
	- storage_connectionString　…Key1の接続文字列

- **Azure Cosmos DB　…キーより転記 ( 4-4で作成 )**
	- cosmos_endpointUri　…URLを転記
	- cosmos_accountKey　…プライマリキーを転記


### 7. **SQL database マイグレーション**

*Visual Studio コンソールで下記コマンドを実行します*  
`update-database`


### 8. **Cognitive Search インデックス / インデクサー の作成**

*下記名前で検索用インデックスを作成します(Azure Portal上で)*  

1. Cognitive Service (検索サービス) の「データのインポート」を選択
2. 各データソース(SQL/Cosmos DB/Tables/Blob)を選択し、インデクサーの作成を進めます

**※重要※**  
- 最低1つの画像ファイルをアップしてから実施してください(Container や Table が自動作成された後にインデックス作成可能です)	
- Tabsフィールドはソート除外にしてください
- インデックス作成対象フィールドはすべて選択でOKです  
  (詳細に設定するには、ModelsのSearchIndexUserMedia～に合わせます)

- Indexer (インデクサー) の名前は下記となります (Settings.cs で変更可能)
	- azuresql-usermedia-index
	- azurecosmos-usermedia-index  
	- azuretable-usermedia-index  
	- azureblob-usermedia-index  

## (画像登録API について)  
*下記URLを利用して画像登録が可能です*

- URL (GET)  
**https://<SITE_DOMAIN>/UserMediaAPI/Import?userId=○○○○○&imagePath=○○○○○&imageFiles=○○○○○**  
- パラメータ
	- userId: 登録先のユーザーID (SQL database [AspNetUsers] テーブルで確認可能)
	- imagePath: 登録する画像のパス (Blob上など)
	- imageFiles: 画像ファイル名 (複数可能)
=======
