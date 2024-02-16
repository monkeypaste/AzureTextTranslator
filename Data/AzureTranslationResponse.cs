using System.Collections.Generic;

namespace AzureTextTranslator {
    public class AzureTranslationResponse {
        public DetectedLanguage detectedLanguage { get; set; }
        public List<Translation> translations { get; set; }
    }
    public class DetectedLanguage {
        public string language { get; set; }
        public string score { get; set; }
    }


    public class Translation {
        public string text { get; set; }
        public string to { get; set; }
    }
}