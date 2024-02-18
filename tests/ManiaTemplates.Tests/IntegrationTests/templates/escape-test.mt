<component>
    <property type="List<int>" name="data"/>

    <template>
        <label foreach="int i in data" if="i > 1 && i < 4" text="{{ i }}" data-cond="{{ i >= 0 }}"/>
        <label foreach="int i in data" if="i < 1 &amp;&amp; i == i" text="{{ i }}" data-cond='{{ i > i }}'/>
    </template>
</component>