<script setup>
import PageHeader from '@/components/PageHeader.vue'
</script>

<template>
  <div class="page-content">
    <v-row class="layout-grid justify-center">
      <v-col cols="4" md="8" lg="6">
        <PageHeader title="Sobre o Monitor de Enchentes" />

        <v-card class="section">
          <v-card-text class="text-body-large">
            <p>
              O Monitor de Enchentes é um mapa colaborativo: qualquer morador pode registrar uma enchente, com
              local, altura da água e foto, sem criar conta. Juntos, esses registros mostram quais áreas alagam
              com mais frequência.
            </p>
            <p class="mt-3">
              As informações ajudam a população, a Defesa Civil e quem procura moradia a conhecer as áreas de
              risco e a planejar a prevenção.
            </p>
          </v-card-text>
        </v-card>

        <v-card class="section">
          <v-card-item>
            <v-card-title class="text-title-large">Como usar</v-card-title>
          </v-card-item>
          <v-list lines="two" class="pt-0">
            <v-list-item title="Toque em “Registrar enchente”" subtitle="Use o GPS ou toque no mapa para marcar o local.">
              <template #prepend>
                <v-avatar color="primary-container" size="32" class="mr-4"><span class="text-label-large">1</span></v-avatar>
              </template>
            </v-list-item>
            <v-list-item title="Diga até onde chegou a água" subtitle="Tornozelo, joelho, cintura ou acima.">
              <template #prepend>
                <v-avatar color="primary-container" size="32" class="mr-4"><span class="text-label-large">2</span></v-avatar>
              </template>
            </v-list-item>
            <v-list-item title="Envie" subtitle="Se quiser, conte o que aconteceu e mande uma foto.">
              <template #prepend>
                <v-avatar color="primary-container" size="32" class="mr-4"><span class="text-label-large">3</span></v-avatar>
              </template>
            </v-list-item>
          </v-list>
        </v-card>

        <v-card class="section" color="surface-variant" elevation="0">
          <v-card-item prepend-icon="mdi-shield-account-outline">
            <v-card-title class="text-title-large">Privacidade</v-card-title>
          </v-card-item>
          <v-card-text class="text-body-large">
            <ul class="ml-4">
              <li>Não pedimos nome, e-mail nem telefone.</li>
              <li>A foto é reduzida no seu celular, e os dados escondidos nela (localização exata, modelo do aparelho) são apagados.</li>
              <li>Contamos as visitas de forma anônima, sem guardar o endereço de internet (IP) de ninguém.</li>
              <li>
                Para você ver as ocorrências que registrou, o app guarda um código aleatório neste aparelho.
                Ele não identifica você e some se você limpar os dados do navegador.
              </li>
              <li>Evite fotografar rostos ou placas de carro.</li>
            </ul>
          </v-card-text>
        </v-card>

        <v-card class="section">
          <v-card-item>
            <v-card-title class="text-title-large">O projeto</v-card-title>
          </v-card-item>
          <v-card-text class="text-body-large">
            <p>
              Projeto de extensão do curso de Análise e Desenvolvimento de Sistemas do Centro Universitário
              Internacional UNINTER, ligado aos Objetivos de Desenvolvimento Sustentável 11 (Cidades e
              comunidades sustentáveis) e 13 (Ação contra a mudança global do clima).
            </p>
            <div class="d-flex flex-wrap ga-2 mt-3">
              <v-chip color="secondary" variant="flat">ODS 11</v-chip>
              <v-chip color="tertiary" variant="flat">ODS 13</v-chip>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>
