using MonkeyPaste.Common.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AzureTextTranslator {
    public class AzureTextTranslatorPlugin :
        MpIAnalyzeComponentAsync,
        MpISupportDeferredValueAsync,
        MpISupportHeadlessAnalyzerFormat {
        const string FROM_PARAM_ID = "from";
        const string TO_PARAM_ID = "to";
        const string TEXT_PARAM_ID = "text";
        const string API_KEY_PARAM_ID = "apikey";
        const string REGION_PARAM_ID = "region";
        const string SIGNUP_PARAM_ID = "signup";

        const string ENDPOINT_URL = "https://api.cognitive.microsofttranslator.com";
        const string SIGNUP_URL = "https://azure.microsoft.com/en-us/free/";

        public async Task<MpAnalyzerPluginResponseFormat> AnalyzeAsync(MpAnalyzerPluginRequestFormat req) {
            // gather param values
            string region = req.GetParamValue(REGION_PARAM_ID);
            string key = req.GetParamValue(API_KEY_PARAM_ID);
            string from_lang = req.GetParamValue(FROM_PARAM_ID);
            string to_lang = req.GetParamValue(TO_PARAM_ID);
            string input_text = req.GetParamValue(TEXT_PARAM_ID);

            var resp = new MpAnalyzerPluginResponseFormat();

            try {
                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage()) {
                    // build rest request

                    request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                    if (!string.IsNullOrWhiteSpace(region)) {
                        request.Headers.Add("Ocp-Apim-Subscription-Region", region);
                    }
                    // use from and to params in uri
                    string route = $"/translate?api-version=3.0&from={from_lang}&to={to_lang}";
                    request.RequestUri = new Uri(ENDPOINT_URL + route);
                    request.Method = HttpMethod.Post;

                    // set request content to input text
                    object[] body = new object[] { new { Text = input_text } };
                    var requestBody = JsonConvert.SerializeObject(body);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                    // submit request
                    HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode) {
                        string result = await response.Content.ReadAsStringAsync();
                        // deserialize response into json objs
                        if (result.DeserializeObject<List<AzureTranslationResponse>>() is { } trl &&
                            trl.FirstOrDefault() is { } trp &&
                            trp.translations.FirstOrDefault() is { } tr) {
                            // set plugin response as 'Text' content with translation result
                            resp.dataObjectLookup = new() {
                                { MpPortableDataFormats.Text, tr.text }
                            };
                        }
                    } else {
                        resp.errorMessage = $"Error code: {(int)response.StatusCode}{Environment.NewLine}{response.ReasonPhrase}";
                    }

                }
            }
            catch (Exception ex) {
                resp.errorMessage = ex.Message;

            }
            return resp;
        }
        public MpAnalyzerComponent GetFormat(MpHeadlessComponentFormatRequest request) {
            Resources.Culture = new System.Globalization.CultureInfo(request.culture);
            return new MpAnalyzerComponent() {
                inputType = new MpPluginInputFormat() {
                    text = true
                },
                outputType = new MpPluginOutputFormat() {
                    text = true
                },
                parameters = new List<MpParameterFormat>() {
                    // EXECUTE PARAMS
                    new MpParameterFormat() {
                        isExecuteParameter = true,
                        isSharedValue = true,
                        isRequired = false,
                        label = Resources.RegionLabel,
                        description = Resources.RegionDescription,
                        controlType = MpParameterControlType.TextBox,
                        unitType = MpParameterValueUnitType.PlainText,
                        paramId = REGION_PARAM_ID,
                    },
                    new MpParameterFormat() {
                        isExecuteParameter = true,
                        isSharedValue = true,
                        isRequired = true,
                        label = Resources.KeyLabel,
                        description = Resources.KeyDescription,
                        controlType = MpParameterControlType.TextBox,
                        unitType = MpParameterValueUnitType.PlainText,
                        paramId = API_KEY_PARAM_ID,
                    },
                    new MpParameterFormat() {
                        isExecuteParameter = true,
                        controlType = MpParameterControlType.Hyperlink,
                        value = new MpParameterValueFormat(SIGNUP_URL,Resources.SignUpLabel,true),
                        paramId = SIGNUP_PARAM_ID,
                    },
                    // ANALYZER FORM PARAMS
                    new MpParameterFormat() {
                        isVisible = false,
                        isRequired = true,
                        label = "Source Content",
                        description = "Text to translate",
                        controlType = MpParameterControlType.TextBox,
                        unitType = MpParameterValueUnitType.PlainTextContentQuery,
                        value = new MpParameterValueFormat("{ClipText}",true),
                        paramId = TEXT_PARAM_ID,
                    },
                    new MpParameterFormat() {
                        label = Resources.FromLabel,
                        isValueDeferred = true,
                        controlType = MpParameterControlType.ComboBox,
                        unitType = MpParameterValueUnitType.PlainTextContentQuery,
                        value = new MpParameterValueFormat(string.Empty,true),
                        paramId = FROM_PARAM_ID,
                    },
                    new MpParameterFormat() {
                        isRequired = true,
                        label = Resources.ToLabel,
                        controlType = MpParameterControlType.ComboBox,
                        unitType = MpParameterValueUnitType.PlainTextContentQuery,
                        isValueDeferred = true,
                        value = new MpParameterValueFormat(string.Empty,true),
                        paramId = TO_PARAM_ID,
                    },
                }
            };
        }

        public async Task<MpDeferredParameterValueResponseFormat> RequestParameterValueAsync(MpDeferredParameterValueRequestFormat req) {
            // simulate async request
            await Task.Delay(1);

            (string, string)[] langs = new[] {
                (Resources.AutoDetectLabel,""),("Afrikaans-Afrikaans","af"),("Amharic-አማርኛ","am"),("Arabic-العربية","ar"),("Assamese-অসমীয়া","as"),("Azerbaijani-Azərbaycan","az"),("Bashkir-Bashkir","ba"),("Bulgarian-Български","bg"),("Bangla-বাংলা","bn"),("Tibetan-བོད་སྐད་","bo"),("Bosnian-Bosnian","bs"),("Catalan-Català","ca"),("Czech-Čeština","cs"),("Welsh-Cymraeg","cy"),("Danish-Dansk","da"),("German-Deutsch","de"),("Divehi-ދިވެހިބަސް","dv"),("Greek-Ελληνικά","el"),("English-English","en"),("Spanish-Español","es"),("Estonian-Eesti","et"),("Persian-فارسی","fa"),("Finnish-Suomi","fi"),("Filipino-Filipino","fil"),("Fijian-NaVosaVakaviti","fj"),("French-Français","fr"),("French(Canada)-Français(Canada)","fr-CA"),("Irish-Gaeilge","ga"),("Gujarati-ગુજરાતી","gu"),("Hebrew-עברית","he"),("Hindi-हिन्दी","hi"),("Croatian-Hrvatski","hr"),("HaitianCreole-HaitianCreole","ht"),("Hungarian-Magyar","hu"),("Armenian-Հայերեն","hy"),("Indonesian-Indonesia","id"),("Inuinnaqtun-Inuinnaqtun","ikt"),("Icelandic-Íslenska","is"),("Italian-Italiano","it"),("Inuktitut-ᐃᓄᒃᑎᑐᑦ","iu"),("Inuktitut(Latin)-Inuktitut(Latin)","iu-Latn"),("Japanese-日本語","ja"),("Georgian-ქართული","ka"),("Kazakh-ҚазақТілі","kk"),("Khmer-ខ្មែរ","km"),("Kurdish(Northern)-Kurdî(Bakur)","kmr"),("Kannada-ಕನ್ನಡ","kn"),("Korean-한국어","ko"),("Kurdish(Central)-Kurdî(Navîn)","ku"),("Kyrgyz-Kyrgyz","ky"),("Lao-ລາວ","lo"),("Lithuanian-Lietuvių","lt"),("Latvian-Latviešu","lv"),("Chinese(Literary)-中文(文言文)","lzh"),("Malagasy-Malagasy","mg"),("Māori-TeReoMāori","mi"),("Macedonian-Македонски","mk"),("Malayalam-മലയാളം","ml"),("Mongolian(Cyrillic)-Mongolian(Cyrillic)","mn-Cyrl"),("Mongolian(Traditional)-ᠮᠣᠩᠭᠣᠯᠬᠡᠯᠡ","mn-Mong"),("Marathi-मराठी","mr"),("Malay-Melayu","ms"),("Maltese-Malti","mt"),("HmongDaw-HmongDaw","mww"),("Myanmar(Burmese)-မြန်မာ","my"),("Norwegian-NorskBokmål","nb"),("Nepali-नेपाली","ne"),("Dutch-Nederlands","nl"),("Odia-ଓଡ଼ିଆ","or"),("QuerétaroOtomi-Hñähñu","otq"),("Punjabi-ਪੰਜਾਬੀ","pa"),("Polish-Polski","pl"),("Dari-دری","prs"),("Pashto-پښتو","ps"),("Portuguese(Brazil)-Português(Brasil)","pt"),("Portuguese(Portugal)-Português(Portugal)","pt-PT"),("Romanian-Română","ro"),("Russian-Русский","ru"),("Slovak-Slovenčina","sk"),("Slovenian-Slovenščina","sl"),("Samoan-GaganaSāmoa","sm"),("Albanian-Shqip","sq"),("Serbian(Cyrillic)-Српски(ћирилица)","sr-Cyrl"),("Serbian(Latin)-Srpski(latinica)","sr-Latn"),("Swedish-Svenska","sv"),("Swahili-Kiswahili","sw"),("Tamil-தமிழ்","ta"),("Telugu-తెలుగు","te"),("Thai-ไทย","th"),("Tigrinya-ትግር","ti"),("Turkmen-TürkmenDili","tk"),("Klingon(Latin)-Klingon(Latin)","tlh-Latn"),("Klingon(pIqaD)-Klingon(pIqaD)","tlh-Piqd"),("Tongan-LeaFakatonga","to"),("Turkish-Türkçe","tr"),("Tatar-Татар","tt"),("Tahitian-ReoTahiti","ty"),("Uyghur-ئۇيغۇرچە","ug"),("Ukrainian-Українська","uk"),("Urdu-اردو","ur"),("Uzbek(Latin)-Uzbek(Latin)","uz"),("Vietnamese-TiếngViệt","vi"),("YucatecMaya-YucatecMaya","yua"),("Cantonese(Traditional)-粵語(繁體)","yue"),("ChineseSimplified-中文(简体)","zh-Hans"),("ChineseTraditional-繁體中文(繁體)","zh-Hant")
            };
            if (req.paramId == FROM_PARAM_ID) {
                // default to auto-detect
                return new MpDeferredParameterValueResponseFormat() {
                    Values =
                    langs.Select((x, idx) => new MpParameterValueFormat(x.Item2, x.Item1, idx == 0)).ToList()
                };
            }
            if (req.paramId == TO_PARAM_ID) {
                // default to first entry and omit auto-detect
                return new MpDeferredParameterValueResponseFormat() {
                    Values =
                    langs.Skip(1).Select((x, idx) => new MpParameterValueFormat(x.Item2, x.Item1, idx == 0)).ToList()
                };
            }
            // non-deferred param (shouldn't happen)
            return null;
        }


    }
}