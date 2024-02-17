﻿<component>
    <using namespace="System.Linq"/>

    <import component="LoopComponent" as="LoopComponent"/>

    <template>
        <LoopComponent foreach="int i in Enumerable.Range(1, 3)">
            <LoopComponent i="{{ i * 10 }}">
                <label text="outer_{{ i }}"/>
            </LoopComponent>
        </LoopComponent>
        <!-- End Loop 1 -->
        <LoopComponent foreach="int i in Enumerable.Range(1, 3)">
            <LoopComponent i="{{ i }}">
                <LoopComponent>
                    <label text="outer" />
                </LoopComponent>
            </LoopComponent>
        </LoopComponent>
        <!-- End Loop 2 -->
        <LoopComponent foreach="int i in Enumerable.Range(1, 2)" i="{{ __index }}">
            <LoopComponent foreach="int j in Enumerable.Range(1, 2)" i="{{ __index2 }}">
                <label text="outer_{{ i }}_{{ j }}"/>
            </LoopComponent>
        </LoopComponent>
    </template>
</component>