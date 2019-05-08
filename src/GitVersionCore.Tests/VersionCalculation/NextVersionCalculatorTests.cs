namespace GitVersionCore.Tests.VersionCalculation
{
    using System;
    using System.Collections.Generic;
    using GitTools.Testing;
    using GitVersion;
    using GitVersion.VersionCalculation;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;

    public class NextVersionCalculatorTests : TestBase
    {
        [Test]
        public void ShouldIncrementVersionBasedOnConfig()
        {
            var baseCalculator = new TestBaseVersionCalculator(true, new SemanticVersion(1), new MockCommit());
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData("ef7d0d7e1e700f1c7c9fa01ea6791bb778a5c37c", 1, "master", "b1a34edbd80e141f7cc046c074f109be7d022074", "b1a34e", DateTimeOffset.Now);
            var sut = new NextVersionCalculator(baseCalculator, new TestMetaDataCalculator(semanticVersionBuildMetaData));
            var config = new Config();
            var context = new GitVersionContextBuilder().WithConfig(config).Build();

            var version = sut.FindVersion(context);

            version.ToString().ShouldBe("1.0.1");
        }

        [Test]
        public void DoesNotIncrementWhenBaseVersionSaysNotTo()
        {
            var baseCalculator = new TestBaseVersionCalculator(false, new SemanticVersion(1), new MockCommit());
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData("ef7d0d7e1e700f1c7c9fa01ea6791bb778a5c37c", 1, "master", "b1a34edbd80e141f7cc046c074f109be7d022074", "b1a34e", DateTimeOffset.Now);
            var sut = new NextVersionCalculator(baseCalculator, new TestMetaDataCalculator(semanticVersionBuildMetaData));
            var config = new Config();
            var context = new GitVersionContextBuilder().WithConfig(config).Build();

            var version = sut.FindVersion(context);

            version.ToString().ShouldBe("1.0.0");
        }

        [Test]
        public void AppliesBranchPreReleaseTag()
        {
            var baseCalculator = new TestBaseVersionCalculator(false, new SemanticVersion(1), new MockCommit());
            var semanticVersionBuildMetaData = new SemanticVersionBuildMetaData("ef7d0d7e1e700f1c7c9fa01ea6791bb778a5c37c", 2, "develop", "b1a34edbd80e141f7cc046c074f109be7d022074", "b1a34e", DateTimeOffset.Now);
            var sut = new NextVersionCalculator(baseCalculator, new TestMetaDataCalculator(semanticVersionBuildMetaData));
            var context = new GitVersionContextBuilder()
                .WithDevelopBranch()
                .Build();

            var version = sut.FindVersion(context);

            version.ToString("f").ShouldBe("1.0.0-alpha.1+2");
        }

        [Test]
        public void PreReleaseTagCanUseBranchName()
        {
            var config = new Config
            {
                NextVersion = "1.0.0",
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "custom", new BranchConfig
                        {
                            Regex = "custom/",
                            Tag = "useBranchName",
                            SourceBranches = new List<string>()
                        }
                    }
                }
            };

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeACommit();
                fixture.BranchTo("develop");
                fixture.MakeACommit();
                fixture.BranchTo("custom/foo");
                fixture.MakeACommit();

                fixture.AssertFullSemver(config, "1.0.0-foo.1+2");
            }

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeACommit();
                fixture.BranchTo("custom/JIRA-123");
                fixture.MakeACommit();
                fixture.MakeACommit();

                fixture.AssertFullSemver(config, "1.0.0-JIRA-123.1+2");
            }
        }

        [Test]
        public void PreReleaseTagCanUseBranchNameWithSeparator()
        {
            var config = new Config
            {
                NextVersion = "1.0.0",
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "custom", new BranchConfig
                        {
                            Regex = "custom/",
                            Tag = "useBranchName",
                            TagSeparator = "_",
                            VersioningMode = VersioningMode.ContinuousDelivery,
                            SourceBranches = new List<string>()
                        }
                    }
                }
            };

            var emptyConfig = new Config
            {
                NextVersion = "1.0.0",
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "custom", new BranchConfig
                        {
                            Regex = "custom/",
                            Tag = "useBranchName",
                            TagSeparator = "",
                            VersioningMode = VersioningMode.ContinuousDelivery,
                            SourceBranches = new List<string>()
                        }
                    }
                }
            };

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeACommit();
                fixture.BranchTo("develop");
                fixture.MakeACommit();
                fixture.BranchTo("custom/foo-bar-baz");
                fixture.MakeACommit();

                fixture.AssertFullSemver(config, "1.0.0-foo_bar_baz.1+2");
            }

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeACommit();
                fixture.BranchTo("develop");
                fixture.MakeACommit();
                fixture.BranchTo("custom/foo-bar.baz");
                fixture.MakeACommit();

                fixture.AssertFullSemver(emptyConfig, "1.0.0-foobarbaz.1+2");
            }

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeACommit();
                fixture.BranchTo("custom/JIRA-123");
                fixture.MakeACommit();
                fixture.MakeACommit();

                fixture.AssertFullSemver(config, "1.0.0-JIRA_123.1+2");
            }
        }

        [Test]
        public void PreReleaseTagCanUseBranchNameVariable()
        {
            var config = new Config
            {
                NextVersion = "1.0.0",
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "custom", new BranchConfig
                        {
                            Regex = "custom/",
                            Tag = "alpha.{BranchName}",
                            SourceBranches = new List<string>()
                        }
                    }
                }
            };

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeACommit();
                fixture.BranchTo("develop");
                fixture.MakeACommit();
                fixture.BranchTo("custom/foo");
                fixture.MakeACommit();

                fixture.AssertFullSemver(config, "1.0.0-alpha.foo.1+2");
            }
        }

        [Test]
        public void PreReleaseNumberShouldBeScopeToPreReleaseLabelInContinuousDelivery()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "master", new BranchConfig()
                        {
                            Tag = "beta"
                        }
                    },
                }
            };

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();

                fixture.Repository.CreateBranch("feature/test");
                Commands.Checkout(fixture.Repository, "feature/test");
                fixture.Repository.MakeATaggedCommit("0.1.0-test.1");
                fixture.Repository.MakeACommit();

                fixture.AssertFullSemver(config, "0.1.0-test.2+2");

                Commands.Checkout(fixture.Repository, "master");
                fixture.Repository.Merge(fixture.Repository.FindBranch("feature/test"), Generate.SignatureNow());

                fixture.AssertFullSemver(config, "0.1.0-beta.1+2");
            }
        }
    }
}
