<component>
    <property type="int" name="i" default="-1" />
    <property type="string" name="unusedArgumentOne" default="hello" />
    
    <template>
        <label text="inner_{{ i }}" />
        <slot />
    </template>
</component>