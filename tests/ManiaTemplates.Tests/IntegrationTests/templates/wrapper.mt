<component>
    <property type="double" name="width" default="20" />
    
    <template>
        <frame size="{{ width }} 11">
            <slot />
        </frame>
    </template>
</component>