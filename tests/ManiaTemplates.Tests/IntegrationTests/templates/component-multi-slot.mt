<component>
    <template>
        <frame class="outer">
            <slot/>
            
            <frame class="inner">
                <slot name="footer"/>
            </frame>
        </frame>
    </template>
</component>
