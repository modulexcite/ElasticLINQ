﻿// Licensed under the Apache 2.0 License. See LICENSE.txt in the project root for more information.

using ElasticLinq.Utility;
using System;

namespace ElasticLinq.Request.Criteria
{
    /// <summary>
    /// Base class for any criteria that maps to a single field.
    /// </summary>
    public abstract class SingleFieldCriteria : ICriteria
    {
        private readonly string field;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleFieldCriteria"/> class.
        /// </summary>
        /// <param name="field">Field this criteria applies to.</param>
        protected SingleFieldCriteria(string field)
        {
            Argument.EnsureNotBlank("field", field);
            this.field = field;
        }

        /// <summary>
        /// Field this criteria applies to.
        /// </summary>
        public string Field
        {
            get { return field; }
        }

        /// <inheritdoc/>
        public abstract string Name
        {
            get;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return String.Format("{0} [{1}]", Name, Field);
        }
    }
}