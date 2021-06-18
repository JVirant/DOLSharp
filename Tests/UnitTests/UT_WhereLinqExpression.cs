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
        public void Filter_NameEqualsConstant()
        {
            var expression = DB.Where<DOLCharacters>(character => character.Name == "Dre");
            var firstQueryParameter = expression.Parameters[0];
            Assert.AreEqual(expression.ParameterizedText, "WHERE Name = " + firstQueryParameter.Name);
            Assert.AreEqual(firstQueryParameter.Value, "Dre");
        }

        [Test]
        public void Filter_LevelEqualsConstant()
        {
            var expression = DB.Where<DOLCharacters>(character => character.Level == 1);

            var firstQueryParameter = expression.Parameters[0];
            Assert.AreEqual(expression.ParameterizedText, "WHERE Level = " + firstQueryParameter.Name);
            Assert.AreEqual(firstQueryParameter.Value, 1);
        }


        [Test]
        public void Filter_NameEqualsConstant_And_LevelEqualsConstant()
        {
            var andExpression = DB.Where<DOLCharacters>(o => o.Name == "Dre"  && o.Level == 2);

            var placeHolder1 = andExpression.Parameters[0].Name;
            var placeHolder2 = andExpression.Parameters[1].Name;
            var actual = andExpression.ParameterizedText;
            var expected = $"WHERE ( Name = {placeHolder1} AND Level = {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Filter_LevelInConstantInt32Values()
        {
            var expr = DB.Where<DOLCharacters>(o => new [] { 1, 2 }.Contains(o.Level));
            var placeHolder1 = expr.Parameters[0].Item1;
            var placeHolder2 = expr.Parameters[1].Item1;
            var actual = expr.ParameterizedText;
            var expected = $"WHERE Level IN ( {placeHolder1} , {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Filter_LevelInConstantStringValues()
        {
            var expr = DB.Where<DOLCharacters>(o => new [] { "a", "b" }.Contains(o.Name));
            var placeHolder1 = expr.Parameters[0].Item1;
            var placeHolder2 = expr.Parameters[1].Item1;
            var actual = expr.ParameterizedText;
            var expected = $"WHERE Name IN ( {placeHolder1} , {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Filter_NameIsNullConstant()
        {
            var expression = DB.Where<DOLCharacters>(o => o.Name == null);
            var actual = expression.ParameterizedText;
            var expected = $"WHERE Name IS NULL";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Filter_NameEqualsVariable()
        {
            var dre = "Dre";
            var expression = DB.Where<DOLCharacters>(character => character.Name == dre);
            var firstQueryParameter = expression.Parameters[0];
            Assert.AreEqual(expression.ParameterizedText, "WHERE Name = " + firstQueryParameter.Name);
            Assert.AreEqual(firstQueryParameter.Value, "Dre");
        }

        [Test]
        public void Filter_LevelEqualsVariable()
        {
            var level = 1;
            var expression = DB.Where<DOLCharacters>(character => character.Level == level);

            var firstQueryParameter = expression.Parameters[0];
            Assert.AreEqual(expression.ParameterizedText, "WHERE Level = " + firstQueryParameter.Name);
            Assert.AreEqual(firstQueryParameter.Value, 1);
        }


        [Test]
        public void Filter_NameEqualsVariable_And_LevelEqualsVariable()
        {
            var dre = "Dre";
            var level = 2;
            var andExpression = DB.Where<DOLCharacters>(o => o.Name == dre  && o.Level == level);

            var placeHolder1 = andExpression.Parameters[0].Name;
            var placeHolder2 = andExpression.Parameters[1].Name;
            var actual = andExpression.ParameterizedText;
            var expected = $"WHERE ( Name = {placeHolder1} AND Level = {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Filter_LevelInInt32Values()
        {
            var levels = new[] {1, 2};
            var expr = DB.Where<DOLCharacters>(o => levels.Contains(o.Level));
            var placeHolder1 = expr.Parameters[0].Item1;
            var placeHolder2 = expr.Parameters[1].Item1;
            var actual = expr.ParameterizedText;
            var expected = $"WHERE Level IN ( {placeHolder1} , {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Filter_LevelInStringValues()
        {
            var names = new List<string> {"a", "b"};
            var expr = DB.Where<DOLCharacters>(o => names.Contains(o.Name));
            var placeHolder1 = expr.Parameters[0].Item1;
            var placeHolder2 = expr.Parameters[1].Item1;
            var actual = expr.ParameterizedText;
            var expected = $"WHERE Name IN ( {placeHolder1} , {placeHolder2} )";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Filter_NameIsNull()
        {
            string name = null;
            var expression = DB.Where<DOLCharacters>(o => o.Name == name);
            var actual = expression.ParameterizedText;
            var expected = $"WHERE Name IS NULL";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Filter_NameEqualsToComplexExpression()
        {
            var names = new [] { "Dre " };
            var expression = DB.Where<DOLCharacters>(o => o.Name == names[0].Trim().ToLower());
            var parameter = expression.Parameters[0];
            var actual = expression.ParameterizedText;
            var expected = $"WHERE Name = {parameter.Name}";
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(names[0].Trim().ToLower(), parameter.Value);
        }

        [Test]
        public void Filter_AutolootIsTrue()
        {
            var expression = DB.Where<DOLCharacters>(o => o.Autoloot);
            var parameter = expression.Parameters[0];
            var actual = expression.ParameterizedText;
            var expected = $"WHERE Autoloot = {parameter.Name}";
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(1, parameter.Value);
        }
    }
}
