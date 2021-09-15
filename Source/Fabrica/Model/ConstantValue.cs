// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace GEAviation.Fabrica.Model
{
    public class ConstantValue : IPartDefOrRef
    {
        public string Value { get; set; }

        public ConstantValue() { }

        /// <summary>
        /// Copy constructor. Generates a deep copy of the specified <see cref="Blueprint"/>.
        /// </summary>
        /// <param name="aToCopy">
        /// The object to copy.
        /// </param>
        public ConstantValue(ConstantValue aToCopy)
        {
            Value = aToCopy.Value;
        }
    }
}
