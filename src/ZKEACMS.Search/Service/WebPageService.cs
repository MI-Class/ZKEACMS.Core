﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Easy;
using Easy.RepositoryPattern;
using Microsoft.EntityFrameworkCore;
using ZKEACMS.Search.Models;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using System.Data;

namespace ZKEACMS.Search.Service
{
    public class WebPageService : ServiceBase<WebPage, WebPageDbContext>, IWebPageService
    {
        public WebPageService(IApplicationContext applicationContext, IOptions<SearchOption> databaseOption) : base(applicationContext)
        {
            DbContext = new WebPageDbContext(databaseOption.Value.ConnectionString);
        }
        public override WebPageDbContext DbContext { get; set; }
        public override DbSet<WebPage> CurrentDbSet => DbContext.WebPage;

        public IEnumerable<WebPage> Search(string q, Pagination pagination)
        {
            var dbConnection = DbContext.Database.GetDbConnection();

            using (var command = dbConnection.CreateCommand())
            {
                command.CommandText = "select COUNT(1) from WebPages T0 inner join containstable(WebPages,Title,@Query) T1 on T0.Url=T1.[KEY]";
                command.Parameters.Add(new SqlParameter("@Query", q));
                if (dbConnection.State != ConnectionState.Open)
                {
                    dbConnection.Open();
                }
                var scalar = command.ExecuteScalar();
                pagination.RecordCount = (int)scalar;
            }

            var result = DbContext.WebPage.FromSql(new RawSqlString(
                              @"select T0.Url,T0.Title,T0.KeyWords,T0.MetaDescription,T0.PageContent,T0.Status,T0.Description,T0.CreateBy,T0.CreatebyName,T0.CreateDate,T0.LastUpdateBy,T0.LastUpdateByName,T0.LastUpdateDate from 
                            WebPages T0 inner join containstable(WebPages,Title,@Query) T1 on T0.Url=T1.[KEY]
                            order by T1.[RANK] DESC
                            OFFSET @PageSize * @PageIndex ROWS FETCH NEXT @PageSize ROWS ONLY;"),
                              new SqlParameter("@Query", q), new SqlParameter("@PageSize", pagination.PageSize), new SqlParameter("@PageIndex", pagination.PageIndex));
            return result.ToList();
        }
    }
}