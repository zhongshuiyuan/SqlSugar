﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
namespace SqlSugar
{
    public abstract class LambadaQueryBuilder : IDMLBuilder
    {
        public LambadaQueryBuilder()
        {

        }
        
        private List<SugarParameter> _QueryPars;
        private List<JoinQueryInfo> _JoinQueryInfos;
        private List<string> _WhereInfos;
        private string _TableNameString;

        public StringBuilder Sql { get; set; }
        public SqlSugarClient Context { get; set; }

        public ISqlBuilder Builder { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public string OrderByValue { get; set; }
        public object SelectValue { get; set; }
        public string SelectCacheKey { get; set; }
        public string EntityName { get; set; }
        public string TableWithString { get; set; }
        public string GroupByValue { get; set; }
        public int WhereIndex { get; set; }
        public int JoinIndex { get; set; }
        public ResolveExpressType ResolveType { get; set; }

        public virtual string SqlTemplate
        {
            get
            {
                return "SELECT {0} FROM {1} {2}";
            }
        }
        public virtual string JoinTemplate
        {
            get
            {
                return " {0} JOIN {1} {2} ON {3} ";
            }
        }
        public virtual string GetTableNameString
        {
            get
            {
                var result= Builder.GetTranslationTableName(EntityName)+TableWithString;
                if (this.TableShortName.IsValuable())
                {
                    result += " " + TableShortName;
                }
                return result;
            }
        }

        public virtual string TableShortName { get; set; }

        public virtual string GetSelectValue
        {
            get
            {
                string reval = string.Empty;
                if (this.SelectValue==null||this.SelectValue is string)
                {
                    reval = GetSelectValueByString();
                }
                else
                {
                    reval = GetSelectValueByExpression();
                }
                if (ResolveType == ResolveExpressType.SelectMultiple) {
                    this.SelectCacheKey = this.SelectCacheKey+string.Join("-",this._JoinQueryInfos.Select(it => it.TableName));
                }
                return reval;
            }
        }
        public virtual string GetSelectValueByExpression()
        {
            var expression = this.SelectValue as Expression;
            ILambdaExpressions resolveExpress = this.Context.LambdaExpressions;
            var isSingle= Builder.LambadaQueryBuilder.JoinQueryInfos.IsValuable();
            resolveExpress.Resolve(expression, ResolveType);
            this.QueryPars.AddRange(resolveExpress.Parameters);
            var reval= resolveExpress.Result.GetResultString();
            this.SelectCacheKey = reval;
            resolveExpress.Clear();
            return reval;
        }
        public virtual string GetSelectValueByString()
        {
            string reval;
            if (this.SelectValue.IsNullOrEmpty())
            {
                string pre = null;
                if (this.JoinQueryInfos.IsValuable() && this.JoinQueryInfos.Any(it => TableShortName.IsValuable()))
                {
                    pre = Builder.GetTranslationColumnName(TableShortName) + ".";
                }
                reval = string.Join(",", this.Context.Database.DbMaintenance.GetColumnInfosByTableName(this.EntityName).Select(it => pre + Builder.GetTranslationColumnName(it.ColumnName)));
            }
            else
            {
                reval = this.SelectValue.ObjToString();
                this.SelectCacheKey = reval;
            }

            return reval;
        }

        public virtual string GetWhereValueString
        {
            get
            {
                if (this.WhereInfos == null) return null;
                else
                {
                    return " WHERE " + string.Join(" ", this.WhereInfos);
                }
            }
        }
        public virtual string GetJoinValueString
        {
            get
            {
                if (this.JoinQueryInfos.IsNullOrEmpty()) return null;
                else
                {
                    return string.Join(" ", this.JoinQueryInfos.Select(it => this.ToJoinString(it)));
                }
            }
        }

        public virtual string ToSqlString()
        {
            Sql = new StringBuilder();
            var tableString = GetTableNameString;
            if (this.JoinQueryInfos.IsValuable())
            {
                tableString = tableString + " " + GetJoinValueString;
            }
            Sql.AppendFormat(SqlTemplate, GetSelectValue, tableString, GetWhereValueString);
            return Sql.ToString();
        }
        public virtual string ToJoinString(JoinQueryInfo joinInfo)
        {
            return string.Format(
                this.JoinTemplate,
                joinInfo.JoinIndex == 1 ? (TableShortName + " " + joinInfo.JoinType.ToString() + " ") : (joinInfo.JoinType.ToString() + " JOIN "),
                joinInfo.TableName,
                joinInfo.ShortName + " " + joinInfo.TableWithString,
                joinInfo.JoinWhere);
        }
        public virtual List<string> WhereInfos
        {
            get
            {
                _WhereInfos = PubMethod.IsNullReturnNew(_WhereInfos);
                return _WhereInfos;
            }
            set { _WhereInfos = value; }
        }

        public virtual List<SugarParameter> QueryPars
        {
            get
            {
                _QueryPars = PubMethod.IsNullReturnNew(_QueryPars);
                return _QueryPars;
            }
            set { _QueryPars = value; }
        }
        public virtual List<JoinQueryInfo> JoinQueryInfos
        {
            get
            {
                _JoinQueryInfos = PubMethod.IsNullReturnNew(_JoinQueryInfos);
                return _JoinQueryInfos;
            }
            set { _JoinQueryInfos = value; }
        }
        public virtual void Clear()
        {
            this.Skip = 0;
            this.Take = 0;
            this.Sql = null;
            this.WhereIndex = 0;
            this.QueryPars = null;
            this.GroupByValue = null;
            this._TableNameString = null;
            this.WhereInfos = null;
            this.JoinQueryInfos = null;
        }
    }
}
