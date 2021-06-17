/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using NUnit.Framework;

using DOL.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.UnitTests.Database
{
    [TestFixture]
    class UT_WhereLinqExpression
    {
        [Test]
        public void FilterClause_KeyColumn_KeyEqualsPlaceHolder()
        {
            var expression = DB.Where<DOLCharacters>(character => character.Name == "Dre");
            var firstQueryParameter = expression.Parameters[0];
            Assert.AreEqual(expression.ParameterizedText, "WHERE Name = " + firstQueryParameter.Name);
            Assert.AreEqual(firstQueryParameter.Value, "Dre");
        }

        [Test]
        public void FilterLinqExpressionWhereQueryParameters_KeyColumnIsEqualToOne_FirstQueryParameterValueIsOne()
        {
            var expression = DB.Where<DOLCharacters>(character => character.Level == 1);

            var firstQueryParameter = expression.Parameters[0];
            Assert.AreEqual(expression.ParameterizedText, "WHERE Level = " + firstQueryParameter.Name);
            Assert.AreEqual(firstQueryParameter.Value, 1);
        }


        [Test]
        public void AndLinqExpressionWhereClause_TwoFilterExpressions_FilterLinqExpressionWhereClausesConnectedWithAND()
        {
            var andExpression = DB.Where<DOLCharacters>(o => o.Name == "Dre"  && o.Level == 2);

            var placeHolder1 = andExpression.Parameters[0].Name;
            var placeHolder2 = andExpression.Parameters[1].Name;
            var actual = andExpression.ParameterizedText;
            var expected = $"WHERE ( Name = {placeHolder1} AND Level = {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void InLinqExpressionWhereClause_WithIntValues()
        {
            var expr = DB.Where<DOLCharacters>(o => new [] { 1, 2 }.Contains(o.Level));
            var placeHolder1 = expr.Parameters[0].Item1;
            var placeHolder2 = expr.Parameters[1].Item1;
            var actual = expr.ParameterizedText;
            var expected = $"WHERE Level IN ( {placeHolder1} , {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void InLinqExpressionWhereClause_WithStringValues()
        {
            var expr = DB.Where<DOLCharacters>(o => new [] { "a", "b" }.Contains(o.Name));
            var placeHolder1 = expr.Parameters[0].Item1;
            var placeHolder2 = expr.Parameters[1].Item1;
            var actual = expr.ParameterizedText;
            var expected = $"WHERE Name IN ( {placeHolder1} , {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void AndLinqExpressionWhereClause_TwoFilterExpressions_FilterExpressionParametersInArray()
        {
            var expression = DB.Where<DOLCharacters>(o => o.Name == null);
            var actual = expression.ParameterizedText;
            var expected = $"WHERE Name IS NULL";
            Assert.AreEqual(expected, actual);
        }
    }
}
