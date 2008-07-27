using System.Reflection;
using NUnit.Framework;
using StructureMap.Attributes;
using StructureMap.Graph;
using StructureMap.Testing.TestData;
using StructureMap.Testing.Widget5;

namespace StructureMap.Testing.Graph
{
    [TestFixture]
    public class SetterInjectionTester
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            DataMother.WriteDocument("FullTesting.XML");
        }

        #endregion

        private PluginGraph getPluginGraph()
        {
            return
                DataMother.BuildPluginGraphFromXml(
                    @"
<StructureMap>
	<Assembly Name='StructureMap.Testing.Widget'/>
	<Assembly Name='StructureMap.Testing.Widget5'/>
	
	<PluginFamily Type='StructureMap.Testing.Widget.IWidget' Assembly='StructureMap.Testing.Widget' DefaultKey='Money'>
		<Source Type='XmlFile' FilePath='FullTesting.XML' XPath='Widgets' NodeName='Widget'/>
		<Plugin Assembly='StructureMap.Testing.Widget' Type='StructureMap.Testing.Widget.NotPluggableWidget' ConcreteKey='NotPluggable'/>
	</PluginFamily>

	<PluginFamily Type='StructureMap.Testing.Widget.Rule' Assembly='StructureMap.Testing.Widget' DefaultKey='Blue'>
		<Instance Key='Bigger' Type='GreaterThan'>
			<Property Name='Attribute' Value='MyDad' />
			<Property Name='Value' Value='10' />
		</Instance>
		<Instance Key='Blue' Type='Color'>
			<Property Name='Color' Value='Blue' />
		</Instance>
		<Instance Key='Red' Type='Color'>
			<Property Name='Color' Value='Red' />
		</Instance>
	</PluginFamily>

	<PluginFamily Type='StructureMap.Testing.Widget5.IGridColumn' Assembly='StructureMap.Testing.Widget5' DefaultKey=''>
		<Source Type='XmlFile' FilePath='GridColumnInstances.XML' XPath='//GridColumns' NodeName='GridColumn'/>
		<Plugin Assembly='StructureMap.Testing.Widget5' Type='StructureMap.Testing.Widget5.OtherGridColumn' ConcreteKey='Other'>
			<Setter Name='ColumnName' />
			<Setter Name='FontStyle' />
			<Setter Name='Rules' />
			<Setter Name='Widget' />
			<Setter Name='WrapLines' />
		</Plugin>
	</PluginFamily>
	
	<Instances/>
</StructureMap>
");
        }

        [Test]
        public void AutoFillDeterminationWithSetterPropertiesIsFalse()
        {
            Plugin plugin = new Plugin(typeof (CannotBeAutoFilledGridColumn));
            Assert.IsFalse(plugin.CanBeAutoFilled);
        }

        [Test]
        public void AutoFillDeterminationWithSetterPropertiesIsTrue()
        {
            Plugin plugin = new Plugin(typeof (AutoFilledGridColumn));
            Assert.IsTrue(plugin.CanBeAutoFilled);
        }

        [Test]
        public void got_the_right_number_of_mandatory_and_optional_properties()
        {
            Plugin plugin = new Plugin(typeof(SetterTarget));
            plugin.Setters.IsMandatory("Name1").ShouldBeFalse();
            plugin.Setters.IsMandatory("Name2").ShouldBeTrue();
            plugin.Setters.IsMandatory("Name3").ShouldBeFalse();
            plugin.Setters.IsMandatory("Name4").ShouldBeTrue();
        }

        [Test]
        public void SetterProperty_picks_up_IsMandatory_from_setter_attribute()
        {
            SetterProperty setter1 = new SetterProperty(ReflectionHelper.GetProperty<SetterTarget>(x => x.Name1));
            setter1.IsMandatory.ShouldBeFalse();

            SetterProperty setter2 = new SetterProperty(ReflectionHelper.GetProperty<SetterTarget>(x => x.Name2));
            setter2.IsMandatory.ShouldBeTrue();
        }


        public class SetterTarget
        {
            public string Name1 { get; set; }
            
            [SetterProperty] 
            public string Name2 { get; set; }
            
            public string Name3 { get; set; }
            
            [SetterProperty]
            public string Name4 { get; set; }
        }

        [Test]
        public void SetterPropertyCollection_builds_the_correct_number_of_properties()
        {
            Plugin plugin = new Plugin(typeof(OtherGridColumn));
            plugin.Setters.OptionalCount.ShouldEqual(7);

            plugin.Setters.Add("Widget");
            plugin.Setters.Add("FontStyle");
            plugin.Setters.Add("ColumnName");
            plugin.Setters.Add("Rules");
            plugin.Setters.Add("WrapLines");

            Assert.AreEqual(2, plugin.Setters.OptionalCount);
            Assert.AreEqual(5, plugin.Setters.MandatoryCount);
        }


        [Test]
        public void CreateSetterPropertyCollectionFromExplicitPlugin()
        {
            PluginGraph pluginGraph = getPluginGraph();
            PluginFamily family = pluginGraph.FindFamily(typeof (IGridColumn));
            Plugin plugin = family.Plugins["Other"];

            Assert.AreEqual(2, plugin.Setters.OptionalCount);
            Assert.AreEqual(5, plugin.Setters.MandatoryCount);
            


            Assert.IsTrue(plugin.Setters.IsMandatory("Widget"));
            Assert.IsTrue(plugin.Setters.IsMandatory("FontStyle"));
            Assert.IsTrue(plugin.Setters.IsMandatory("ColumnName"));
            Assert.IsTrue(plugin.Setters.IsMandatory("Rules"));
            Assert.IsTrue(plugin.Setters.IsMandatory("WrapLines"));
        }

        [Test]
        public void CreateSetterPropertyCollectionFromImplicitPlugin()
        {
            /*    The BasicGridColumn class has 5 [SetterProperty] marked properties
			 *    Widget
			 *    FontStyle
			 *    ColumnName
			 *    Rules
			 *    WrapLines
			 */

            Plugin plugin = new Plugin(typeof (BasicGridColumn));
            Assert.AreEqual(5, plugin.Setters.MandatoryCount);
            Assert.IsTrue(plugin.Setters.IsMandatory("Widget"));
            Assert.IsTrue(plugin.Setters.IsMandatory("FontStyle"));
            Assert.IsTrue(plugin.Setters.IsMandatory("ColumnName"));
            Assert.IsTrue(plugin.Setters.IsMandatory("Rules"));
            Assert.IsTrue(plugin.Setters.IsMandatory("WrapLines"));
        }

        [Test]
        public void Log_240_when_a_designated_setter_does_not_exist()
        {
            string errorXml =
                @"
                <StructureMap>
	                <PluginFamily Type='StructureMap.Testing.Widget5.IGridColumn' Assembly='StructureMap.Testing.Widget5' DefaultKey=''>
		                <Plugin Assembly='StructureMap.Testing.Widget5' Type='StructureMap.Testing.Widget5.OtherGridColumn' ConcreteKey='Other'>
			                <Setter Name='Does not exist' />
		                </Plugin>
	                </PluginFamily>
                </StructureMap>
                ";


            PluginGraph graph = DataMother.BuildPluginGraphFromXml(errorXml);
            graph.Log.AssertHasError(240);
        }


        [Test]
        public void Log_241_when_a_designated_setter_does_not_exist()
        {
            string errorXml =
                @"
                <StructureMap>
	                <PluginFamily Type='StructureMap.Testing.Widget5.IGridColumn' Assembly='StructureMap.Testing.Widget5' DefaultKey=''>
		                <Plugin Assembly='StructureMap.Testing.Widget5' Type='StructureMap.Testing.Widget5.OtherGridColumn' ConcreteKey='Other'>
			                <Setter Name='ReadOnly' />
		                </Plugin>
	                </PluginFamily>
                </StructureMap>
                ";


            PluginGraph graph = DataMother.BuildPluginGraphFromXml(errorXml);
            graph.Log.AssertHasError(240);
        }

        [Test, ExpectedException(typeof (StructureMapException))]
        public void TryToAddANonExistentSetterProperty()
        {
            Plugin plugin = new Plugin(typeof (BasicGridColumn), "Basic");
            plugin.Setters.Add("NonExistentPropertyName");
        }

        [Test, ExpectedException(typeof (StructureMapException))]
        public void TryToAddASetterPropertyThatDoesNotHaveASetter()
        {
            Plugin plugin = new Plugin(typeof (BasicGridColumn), "Basic");
            plugin.Setters.Add("HeaderText");
        }

    }
}