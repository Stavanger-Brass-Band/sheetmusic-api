# Changelog

## [0.1.1](https://github.com/Stavanger-Brass-Band/sheetmusic-api/compare/v0.1.0...v0.1.1) (2026-07-02)


### Features

* migrate to ASP.NET Core Identity framework (non-breaking) ([#122](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/122)) ([3a1f9e3](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/3a1f9e31088b2d6bc70563198f93b30b0462a87c))
* password reset email flow via Resend ([#157](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/157)) ([e32698a](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/e32698a76580466fa7d47d7454d0f065ea31ed6c))
* rate limit forgot-password and login endpoints ([#170](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/170)) ([004bdbf](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/004bdbfaf1bd4631d5bf58e2eb7b22b496e8a89b))


### Bug Fixes

* apply $orderby to GET /sheetmusic/sets ([#169](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/169)) ([8e6251f](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/8e6251f212af56bafcc37372d1a8107f21f9da5a))
* enforce account lockout after repeated failed login attempts ([#171](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/171)) ([9b08ceb](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/9b08ceb59d95279cd3ec1d5ccbeda594a081c3e5))
* resolve v1 auth 401s for legacy musicians unlinked to ApplicationUser ([#176](https://github.com/Stavanger-Brass-Band/sheetmusic-api/issues/176)) ([62f5375](https://github.com/Stavanger-Brass-Band/sheetmusic-api/commit/62f537579c8a32bb190f73cf414c901c32bdb807))

## 0.1.0 (2026-05-20)

- Initial release
