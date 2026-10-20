<component>
    <property type="List<int>" name="data"/>

    <template>
        <frame foreach="int i in data">
            <label if="i > 1 && i < 4" text="{{ i }}" data-cond="{{ i >= 0 }}"/>
        </frame>
        <frame foreach="int i in data">
            <label if="i < 1 &amp;&amp; i == i" text="{{ i }}" data-cond='{{ i > i }}'/>
        </frame>
    </template>
</component>