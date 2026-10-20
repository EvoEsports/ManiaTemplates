<component>
    <import component="ItemList" as="ItemList"/>

    <property type="string" name="prefix"/>

    <template>
        <ItemList foreach="int i in new[] { 1, 2 }" items="{{ new List<string> { &quot;a&quot;, &quot;b&quot; } }}" title="List {{ i }}">
            <label text="{{ prefix }}{{ i }}: {{ item }} at {{ index }} in {{ title }} ({{ __index }})"/>
            <label foreach="int n in new[] { 7 }" text="{{ item }} {{ n }} at {{ __index2 }}"/>
        </ItemList>
    </template>
</component>
