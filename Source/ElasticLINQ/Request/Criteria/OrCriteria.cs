﻿// Licensed under the Apache 2.0 License. See LICENSE.txt in the project root for more information.

using ElasticLinq.Utility;
using System.Collections.Generic;
using System.Linq;

namespace ElasticLinq.Request.Criteria
{
    /// <summary>
    /// Criteria that requires one of the criteria to be
    /// satisfied in order to select the document.
    /// </summary>
    internal class OrCriteria : CompoundCriteria
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrCriteria"/> class.
        /// </summary>
        /// <param name="criteria">Criteria to combine with 'or' semantics.</param>
        /// <remarks>Consider using <see cref="OrCriteria.Combine(ICriteria[])"/> instead.</remarks>
        public OrCriteria(params ICriteria[] criteria)
            : base(criteria)
        {
        }

        /// <inheritdoc/>
        public override string Name
        {
            get { return "or"; }
        }

        /// <summary>
        /// Combine a number of <see cref="ICriteria" /> with 'or' semantics.
        /// </summary>
        /// <param name="criteria">The <see cref="ICriteria" /> to be combined.</param>
        /// <returns><see cref="ICriteria" /> representing the original passed <see cref="ICriteria" /> with 'or' semantics.</returns>
        /// <remarks>This is usually an <see cref="OrCriteria" /> but might not be if the passed criteria can be collapsed into
        /// a single criteria.</remarks>
        public static ICriteria Combine(params ICriteria[] criteria)
        {
            Argument.EnsureNotNull("criteria", criteria);

            criteria = FlattenOrCriteria(criteria).ToArray();
            return CombineTermsForSameField(criteria) ?? new OrCriteria(criteria);
        }

        /// <summary>
        /// Flatten a tree of nested <see cref="OrCriteria" /> into a single <see cref="OrCriteria" />.
        /// </summary>
        /// <param name="criteria">List of <see cref="ICriteria" /> to be flattened.</param>
        /// <returns>Flattened list of <see cref="ICriteria" />.</returns>
        /// <remarks>
        /// This is necessary as the compiler-generated unary expression tree appears as ((a || b) || c).
        /// We we would like the simpler form that looks more like the original source of (a || b || c).
        /// </remarks>
        private static IEnumerable<ICriteria> FlattenOrCriteria(IEnumerable<ICriteria> criteria)
        {
            foreach (var criterion in criteria)
            {
                if (criterion is OrCriteria)
                    foreach (var subCriterion in ((OrCriteria)criterion).Criteria)
                        yield return subCriterion;
                else
                    yield return criterion;
            }
        }

        /// <summary>
        /// Takes a collection of <see cref="ICriteria" /> and if they are all 
        /// <see cref="ITermsCriteria" /> for the same field replaces them with a single
        /// <see cref="ITermsCriteria" /> containing all terms for that field.
        /// </summary>
        /// <param name="criteria">collection of <see cref="ICriteria" /> that might be combined.</param>
        /// <returns><see cref="ITermsCriteria" /> containing all terms for that field or null if they can not be combined.</returns>
        private static ICriteria CombineTermsForSameField(ICollection<ICriteria> criteria)
        {
            if (criteria.Count <= 1) return null;

            var termCriteria = criteria.OfType<ITermsCriteria>().ToArray();
            var areAllSameTerm = termCriteria.Length == criteria.Count
                                 && termCriteria.Select(f => f.Field).Distinct().Count() == 1
                                 && termCriteria.All(f => f.IsOrCriteria);

            return areAllSameTerm
                ? TermsCriteria.Build(termCriteria[0].Field, termCriteria[0].Member, termCriteria.SelectMany(f => f.Values).Distinct())
                : null;
        }
    }
}