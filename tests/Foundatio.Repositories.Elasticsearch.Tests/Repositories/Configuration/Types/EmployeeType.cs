﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundatio.Logging;
using Foundatio.Parsers.ElasticQueries;
using Foundatio.Parsers.ElasticQueries.Extensions;
using Foundatio.Repositories.Elasticsearch.Configuration;
using Foundatio.Repositories.Elasticsearch.Extensions;
using Foundatio.Repositories.Elasticsearch.Queries.Builders;
using Foundatio.Repositories.Elasticsearch.Tests.Repositories.Models;
using Foundatio.Repositories.Elasticsearch.Tests.Repositories.Queries;
using Nest;

namespace Foundatio.Repositories.Elasticsearch.Tests.Repositories.Configuration.Types {
    public class EmployeeType : IndexTypeBase<Employee> {
        public EmployeeType(IIndex index) : base(index) { }

        public override TypeMappingDescriptor<Employee> BuildMapping(TypeMappingDescriptor<Employee> map) {
            return base.BuildMapping(map
                .Dynamic(false)
                .Properties(p => p
                    .SetupDefaults()
                    .Keyword(f => f.Name(e => e.Id))
                    .Keyword(f => f.Name(e => e.CompanyId))
                    .Keyword(f => f.Name(e => e.CompanyName))
                    .Text(f => f.Name(e => e.Name).Fields(f1 => f1.Keyword(s => s.Name("keyword").IgnoreAbove(256))))
                    .Scalar(f => f.Age, f => f.Name(e => e.Age).Alias("aliasedage"))
                    .Scalar(f => f.NextReview, f => f.Name(e => e.NextReview).Alias("next"))
                    .GeoPoint(f => f.Name(e => e.Location))
                    .Object<Dictionary<string, object>>(f => f.Name(e => e.Data).Properties(p1 => p1
                        .Object<object>(f2 => f2.Name("@user_meta").Properties(p2 => p2
                            .Text(f3 => f3.Name("twitter_id").RootAlias("twitter").IncludeInAll().Boost(1.1).Fields(f4 => f4.Keyword(f5 => f5.Name("keyword"))))
                            .Number(f3 => f3.Name("twitter_followers").RootAlias("followers").IncludeInAll().Boost(1.1))))
                        ))
                    .Nested<PeerReview>(f => f.Name(e => e.PeerReviews).Properties(p1 => p1
                        .Keyword(f2 => f2.Name(p2 => p2.ReviewerEmployeeId))
                        .Number(f2 => f2.Name(p3 => p3.Rating).Type(NumberType.Integer))))
                    ));
        }

        protected override void ConfigureQueryBuilder(ElasticQueryBuilder builder) {
            builder.Register<AgeQueryBuilder>();
            builder.Register<CompanyQueryBuilder>();
        }

        protected override void ConfigureQueryParser(ElasticQueryParserConfiguration config) {
            config.UseIncludes(i => ResolveIncludeAsync(i));
        }

        private async Task<string> ResolveIncludeAsync(string name) {
            await Task.Delay(100);
            return "aliasedage:10";
        }
    }

    public class EmployeeTypeWithYearsEmployed : EmployeeType {
        public EmployeeTypeWithYearsEmployed(IIndex index) : base(index: index) { }

        public override TypeMappingDescriptor<Employee> BuildMapping(TypeMappingDescriptor<Employee> map) {
            return base.BuildMapping(map
                .Dynamic(false)
                .Properties(p => p
                    .SetupDefaults()
                    .Keyword(f => f.Name(e => e.Id))
                    .Keyword(f => f.Name(e => e.CompanyId))
                    .Keyword(f => f.Name(e => e.CompanyName))
                    .Text(f => f.Name(e => e.Name).Fields(f1 => f1.Keyword(s => s.Name("keyword").IgnoreAbove(256))))
                    .Scalar(f => f.Age, f => f.Name(e => e.Age))
                    .Scalar(f => f.YearsEmployed, f => f.Name(e => e.YearsEmployed))
                    .Date(f => f.Name(e => e.LastReview))
                    .Scalar(f => f.NextReview, f => f.Name(e => e.NextReview).Alias("next"))
                ));
        }
    }

    public class EmployeeTypeWithWithPipeline : EmployeeType, IHavePipelinedIndexType {
        public EmployeeTypeWithWithPipeline(IIndex index) : base(index: index) { }

        public string Pipeline { get; } = "increment-age-pipeline";

        public override async Task ConfigureAsync() {
            var response = await Configuration.Client.PutPipelineAsync(Pipeline, d => d
                .Processors(p => p
                    .Lowercase<Employee>(l => l.Field(f => f.Name))
                    .Trim<Employee>(t => t.Field(f => f.Name))
                ).OnFailure(of => of.Set<Employee>(s => s.Field(f => f.Name).Value(String.Empty))));

            var logger = Configuration.LoggerFactory.CreateLogger(typeof(EmployeeTypeWithWithPipeline));
            logger.Trace(() => response.GetRequest());
            if (response.IsValid)
                return;

            string message = $"Error creating the pipeline {Pipeline}: {response.GetErrorMessage()}";
            logger.Error().Exception(response.OriginalException).Message(message).Property("request", response.GetRequest()).Write();
            throw new ApplicationException(message, response.OriginalException);
        }
    }

    public class DailyEmployeeType : DailyIndexType<Employee> {
        public DailyEmployeeType(IIndex index) : base(index: index) { }

        public override TypeMappingDescriptor<Employee> BuildMapping(TypeMappingDescriptor<Employee> map) {
            return base.BuildMapping(map
                .Dynamic(false)
                .Properties(p => p
                    .SetupDefaults()
                    .Keyword(f => f.Name(e => e.Id))
                    .Keyword(f => f.Name(e => e.CompanyId))
                    .Keyword(f => f.Name(e => e.CompanyName))
                    .Text(f => f.Name(e => e.Name).Fields(f1 => f1.Keyword(s => s.Name("keyword").IgnoreAbove(256))))
                    .Scalar(f => f.Age, f => f.Name(e => e.Age))
                    .Date(f => f.Name(e => e.LastReview))
                    .Scalar(f => f.NextReview, f => f.Name(e => e.NextReview).Alias("next"))
                ));
        }

        protected override void ConfigureQueryBuilder(ElasticQueryBuilder builder) {
            builder.Register<AgeQueryBuilder>();
            builder.Register<CompanyQueryBuilder>();
        }
    }

    public class MonthlyEmployeeType : MonthlyIndexType<Employee> {
        public MonthlyEmployeeType(IIndex index) : base(index: index) { }

        public override TypeMappingDescriptor<Employee> BuildMapping(TypeMappingDescriptor<Employee> map) {
            return base.BuildMapping(map
                .Dynamic(false)
                .Properties(p => p
                    .SetupDefaults()
                    .Keyword(f => f.Name(e => e.Id))
                    .Keyword(f => f.Name(e => e.CompanyId))
                    .Keyword(f => f.Name(e => e.CompanyName))
                    .Text(f => f.Name(e => e.Name).Fields(f1 => f1.Keyword(s => s.Name("keyword").IgnoreAbove(256))))
                    .Scalar(f => f.Age, f => f.Name(e => e.Age))
                    .Date(f => f.Name(e => e.LastReview))
                    .Scalar(f => f.NextReview, f => f.Name(e => e.NextReview).Alias("next"))
                ));
        }

        protected override void ConfigureQueryBuilder(ElasticQueryBuilder builder) {
            builder.Register<AgeQueryBuilder>();
            builder.Register<CompanyQueryBuilder>();
        }
    }
}