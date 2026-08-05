# Changelog

## [0.3.0](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.2.6...v0.3.0) (2026-08-05)


### ⚠ BREAKING CHANGES

* **users:** remove legacy v1 user handling ([#285](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/285))

### Features

* **ai:** complete metadata enrichment agent ([#308](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/308)) ([ae028ac](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/ae028ac35a238d17ca5e1ff042e466a3c7dbf656))
* **auth:** enforce music catalog access scopes ([#317](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/317)) ([f7df166](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/f7df16614cbb227b0f62d95643e7629f273ce654))
* **foundry:** add project for model deployments ([#310](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/310)) ([0d7567b](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/0d7567bec957737b37ab443464a2adb1f164abbc))
* **foundry:** wire agents to project resource ([#312](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/312)) ([8c6e2d0](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/8c6e2d011437453a945b8ae86e84ab1cb6812e4d))
* **parts:** add instrument group classification ([#318](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/318)) ([adaacef](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/adaaceff9a32283316b277c3ba5b05b29f5d92cc))
* **projects:** add Prosjektleder role for managing projects ([#304](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/304)) ([86f2459](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/86f2459d7a6de3c98deec22ab6d3c63a37b2613b))
* **users:** assign parts to musicians ([#323](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/323)) ([5eae072](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/5eae072582e0cd6824e778b9ee80d95a21f725c7))
* **users:** remove legacy v1 user handling ([#285](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/285)) ([6963c3a](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/6963c3af7ed46cfc4d6dbcec20ce991bf10ef181))


### Bug Fixes

* **auth:** seed arkivleser role ([#322](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/322)) ([834cb1d](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/834cb1d2c11f75cd0d242d78700d00b3926d78d6))
* **deploy:** assign resources to ACA compute environment ([#311](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/311)) ([a0365c2](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/a0365c2e6379b065662ecd9180fce18da6b2a6f9))
* **foundry:** deploy model in Sweden Central ([#309](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/309)) ([cd20ef5](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/cd20ef5352184458ff5204394c8f6eb1f7bf0521))
* **projects:** apply \ filter on GET /projects ([#306](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/306)) ([6a81f2a](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/6a81f2aae6e4cf149937339c40f3a37b9a1c0ce2))
* **projects:** show inactive projects to project managers ([#325](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/325)) ([b2df44d](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/b2df44d7a83d0dc40d49bdb3ea14f20b43053ec0))
* **users:** include roles in user list ([#324](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/324)) ([a5b581c](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/a5b581c8483ddb17d28c49cdc7b91654b8c9f474))
* **users:** persist profile updates ([#321](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/321)) ([c13696b](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/c13696b74bc417bdeb7bff3dd8ee6cfb354518ca))

## [0.2.6](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.2.5...v0.2.6) (2026-08-03)


### Bug Fixes

* **apphost:** switch prod SQL database to Basic tier ([#300](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/300)) ([f68382f](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/f68382f09acf9f075b332286fd4aeedc2c29ce28))

## [0.2.5](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.2.4...v0.2.5) (2026-08-02)


### Bug Fixes

* **cors:** allowing noter.stavanger-brassband.no to cors ([#298](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/298)) ([f1188c3](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/f1188c3ddda683144f751ecdc7aa8a0626466810))

## [0.2.4](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.2.3...v0.2.4) (2026-08-02)


### Bug Fixes

* **database:** retry transient SQL failures and stop prod free-limit auto-pause ([#296](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/296)) ([6ce8d14](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/6ce8d143e852b1ada8a23f92dd4ff8095abb9eac))

## [0.2.3](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.2.2...v0.2.3) (2026-07-31)


### Bug Fixes

* **auth:** accept grant_type=password as alias for basic grant ([#281](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/281)) ([7036779](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/70367798fa6875c0ba0fb6e7ab9e3815f9c6a72b))
* **auth:** assume basic grant_type when omitted ([#280](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/280)) ([3e252b9](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/3e252b9a74ad35414e07c9ca85b56c70222973b5))
* **search:** treat rebuild-index as idempotent when the index does not yet exist ([#282](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/282)) ([8911948](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/8911948b5f1290b88c862ab33669b6abb65d3729))
* **sets:** validate title on set creation and persist recording url ([#284](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/284)) ([b0ca2a7](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/b0ca2a713c0648880bf27693402a54f0f8ad0792))
* **users:** detach connected musician before hard-deleting a user ([#277](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/277)) ([aa05c4d](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/aa05c4d97dea66f5847254390c5fdec2acb9cbac))

## [0.2.2](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.2.1...v0.2.2) (2026-07-29)


### Bug Fixes

* **cors:** declare a single CORS policy and enforce it correctly ([#274](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/274)) ([a9536f5](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/a9536f53acabb4a2f721ecadaa99c91ca09c92bc))
* **odata:** apply $orderby, $skip and $top on GET /projects and /parts ([#272](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/272)) ([d55ab08](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/d55ab08fb24720b67773305ce2919fc585df4083))
* **parts:** return 409 conflict instead of 500 when deleting a part in use ([#270](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/270)) ([4f9e089](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/4f9e08999b73e167da8bb04bedd9d84f3b21e520))

## [0.2.1](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.2.0...v0.2.1) (2026-07-29)


### Bug Fixes

* **deploy:** use prod environment name to match federated identity credential ([#266](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/266)) ([09c7a1d](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/09c7a1d89c4a4eb3070b6ce42c7b6088aa7a5fab))

## [0.2.0](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.1.0...v0.2.0) (2026-07-29)


### ⚠ BREAKING CHANGES

* **users:** introduce Noteansvarlig role for three-tier authorization ([#258](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/258))
* **docs:** migrate API documentation from Swashbuckle to Scalar ([#232](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/232))
* **projects:** support reordering sets via POST sets endpoint ([#207](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/207))

### Features

* **auth:** issue and rotate refresh tokens on login ([#257](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/257)) ([48f8988](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/48f8988e391cdb53f30e205450f5274b3688e867))
* **categories:** add category management and set categorization ([#200](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/200)) ([0f018f8](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/0f018f87ccf2319c7399cea2d396e3e4de57d678))
* **deploy:** provision Search, ACR, and shared test/prod ACA environment via Aspire ([#259](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/259)) ([8f54bd7](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/8f54bd7790726811ed74d44bde1f95217179ecc8))
* **docs:** migrate API documentation from Swashbuckle to Scalar ([#232](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/232)) ([b2171e1](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/b2171e11640b8f848a8c82a4c82f8d2868309e40))
* implement phase 0 infrastructure prerequisites for cost-reduction migration ([#252](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/252)) ([87bfb47](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/87bfb4721857408e6e8e3b96de9020c8ea2a7d42))
* migrate to ASP.NET Core Identity framework (non-breaking) ([#122](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/122)) ([3a1f9e3](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/3a1f9e31088b2d6bc70563198f93b30b0462a87c))
* password reset email flow via Resend ([#157](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/157)) ([e32698a](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/e32698a76580466fa7d47d7454d0f065ea31ed6c))
* **projects:** add comments field to project creation endpoint ([#189](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/189)) ([0ad8ee6](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/0ad8ee6cc2b2c161ec2c33c51c5efd0cbeaf84fe))
* **projects:** support reordering sets via POST sets endpoint ([#207](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/207)) ([c33fd06](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/c33fd068a3de6babbc4c27a718fa3b6fc925edea))
* **projects:** support sorting sets within a project ([#202](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/202)) ([c2f476f](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/c2f476fe5215eb746941e9928e77d2772a08f507))
* rate limit forgot-password and login endpoints ([#170](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/170)) ([004bdbf](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/004bdbfaf1bd4631d5bf58e2eb7b22b496e8a89b))
* **users:** admin endpoints for user handling ([#173](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/173)) ([a52ea7d](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/a52ea7d0768ab793aa006a6014b3e768a5fa647a))
* **users:** introduce Noteansvarlig role for three-tier authorization ([#258](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/258)) ([0da4c2a](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/0da4c2afe72c07bcf4980f8edce543abe89cfe0d))
* **users:** report actionable password requirement errors and expose the policy ([#255](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/255)) ([6ad9ce8](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/6ad9ce828d3dcde410a2ec90ea2acbdc7c1ab98c))


### Bug Fixes

* apply $orderby to GET /sheetmusic/sets ([#169](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/169)) ([8e6251f](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/8e6251f212af56bafcc37372d1a8107f21f9da5a))
* **blob-storage:** avoid disposing shared BlobServiceClient registration in data protection setup ([#254](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/254)) ([70116e5](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/70116e5510d53939c1bb4b1acc2921974eeecd81))
* **deploy:** pin Azure.Identity to a version supporting AZURE_TOKEN_CREDENTIALS=ManagedIdentityCredential ([#264](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/264)) ([3d86b1e](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/3d86b1e700f4fabd2c6be169c7cb9e9b8524114b))
* **deploy:** pin migration job/API connection name to SheetMusicContext ([#262](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/262)) ([358501c](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/358501c8b745e8366f56a83280cbde9383e2f321))
* **deploy:** re-provision migration container app jobs before starting them ([#263](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/263)) ([a00b015](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/a00b0157f0b8adac65becd5433628c74e91135ae))
* **deploy:** use vars instead of secrets for Azure identity in login step ([#260](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/260)) ([770c8a0](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/770c8a07777899d14f4489781d02c9ca0de7b0a5))
* enforce account lockout after repeated failed login attempts ([#171](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/171)) ([9b08ceb](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/9b08ceb59d95279cd3ec1d5ccbeda594a081c3e5))
* **odata:** document $orderby correctly in Swagger and reject JSON clauses ([#219](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/219)) ([a290ae0](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/a290ae0bac547d22e476e4d1cfe9a4864ff46fb2))
* **parts:** support $expand=aliases on GET /parts ([#231](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/231)) ([59332ff](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/59332ff354f060331d9bb10d210fd745897c698a))
* remove Newtonsoft.Json and revert incompatible Microsoft.OpenApi bump ([#222](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/222)) ([b0a4210](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/b0a4210d5ebfb1f48a92b9676f7cf9b2bb7cdca1))
* resolve v1 auth 401s for legacy musicians unlinked to ApplicationUser ([#176](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/176)) ([62f5375](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/62f537579c8a32bb190f73cf414c901c32bdb807))
* **sets:** return 400 instead of bare 500 for invalid OData query params ([#206](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/206)) ([0d6342f](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/0d6342fe1d90357c53159082ecb2751307d96e50))

## 0.1.0 (2026-05-20)

- Initial release
