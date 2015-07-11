#region Arx One FTP
// Arx One FTP
// A simple FTP client
// https://github.com/ArxOne/FTP
// Released under MIT license http://opensource.org/licenses/MIT
#endregion

namespace ArxOne.Ftp
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents FTP server features
    /// </summary>
    public class FtpServerFeatures
    {
        private readonly IDictionary<string, IList<string>> _features;

        /// <summary>
        /// Gets the features.
        /// </summary>
        /// <value>
        /// The features.
        /// </value>
        public IEnumerable<string> Features { get { return _features.Keys; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpServerFeatures"/> class.
        /// </summary>
        /// <param name="replyList">The reply list.</param>
        internal FtpServerFeatures(IEnumerable<string> replyList)
        {
            _features = new Dictionary<string, IList<string>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var reply in replyList)
            {
                var parametersIndex = reply.IndexOf(' ');
                if (parametersIndex < 0)
                    RegisterFeature(reply);
                else
                {
                    var feature = reply.Substring(0, parametersIndex);
                    var parameters = reply.Substring(parametersIndex + 1);
                    RegisterFeature(feature, parameters);
                }
            }
        }

        /// <summary>
        /// Registers the feature.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="parameters">The parameters.</param>
        private void RegisterFeature(string feature, string parameters = null)
        {
            if (!_features.ContainsKey(feature))
                _features[feature] = new List<string>();
            if (parameters != null)
                _features[feature].Add(parameters);
        }

        /// <summary>
        /// Determines whether the specified feature is present.
        /// </summary>
        /// <param name="feature">The request feature.</param>
        /// <returns></returns>
        public bool HasFeature(string feature)
        {
            return _features.ContainsKey(feature);
        }

        /// <summary>
        /// Gets the feature parameters.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <returns></returns>
        public IList<string> GetFeatureParameters(string feature)
        {
            IList<string> parameters;
            _features.TryGetValue(feature, out parameters);
            return parameters;
        }
    }
}
