<component>
    <property type="List<string>" name="items"/>
    <property type="string" name="title" default="List"/>

    <template>
        <frame>
            <label text="{{ title }}"/>
            <template foreach="string item in items">
                <slot item="{{ item }}" index="{{ __index }}" title="{{ title }}"/>
            </template>
        </frame>
    </template>
</component>
