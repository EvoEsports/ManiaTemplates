<component>
    <using namespace="System.Linq" />
    <import component="FallthroughComponent" as="FallthroughComponent"/>
    <property type="string" name="testString" />
    <property type="int" name="index" />

    <template>
        <FallthroughComponent foreach="int i in Enumerable.Range(1, 3)" data-index="{{ i }}" index="{{ i * 10 }}" data-test="unit"/>
    </template>
</component>