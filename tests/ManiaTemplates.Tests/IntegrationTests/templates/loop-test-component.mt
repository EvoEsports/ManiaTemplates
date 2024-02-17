<component>
    <property type="int" name="i" default="-1" />
    
    <template>
        <label text="inner_{{ i }}" />
        <slot />
    </template>
</component>