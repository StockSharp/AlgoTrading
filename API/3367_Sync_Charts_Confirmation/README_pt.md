# Estratégia de confirmação de gráficos de sincronização
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reflete a ideia do utilitário original MQL "SyncCharts" monitorando dois feeds de velas do mesmo instrumento e
tomar decisões de negociação somente quando ambos os fluxos confirmarem a mesma direção de tendência. A série master atua como gráfico de referência
(aquele que um trader normalmente observa), enquanto a série seguidora representa um gráfico auxiliar (por exemplo, um período de tempo mais rápido ou
uma agregação alternativa). Ao forçar ambos os fluxos a concordarem antes de entrar no mercado, o sistema filtra o ruído proveniente de
dessincronização temporária entre intervalos do gráfico.

A configuração funciona melhor em instrumentos que exibem uma estrutura de tendência de vários períodos, como futuros de índices e pares de moedas líquidas.
Como ambos os gráficos devem mover-se juntos antes de uma negociação ser realizada, os sinais falsos são reduzidos e a estratégia limita naturalmente
exposição durante as fases caóticas do mercado, quando os prazos discordam ou novas velas são impressas em momentos diferentes.

## Detalhes

- **Critérios de entrada**:
  - **Longo**: As médias móveis simples (SMAs) mestre e seguidor inclinam-se para cima em suas velas finalizadas mais recentes, e
os carimbos de data e hora dessas velas diferem menos que a tolerância de sincronização.
  - **Curto**: Ambos os SMAs têm inclinação descendente e a diferença do carimbo de data/hora está dentro da janela de tolerância.
- **Critérios de saída**:
  - Dessincronização de tempo: se as últimas velas estiverem separadas por mais do que a tolerância permitida, a posição é achatada.
  - Desacordo de tendência: se um SMA subir enquanto o outro descer, a posição aberta é fechada imediatamente.
- **Paradas**: a lógica de nivelamento implícita atua como uma parada suave. Nenhuma parada brusca separada é enviada.
- **Longo/Curto**: Ambos os lados são negociados.
- **Valores padrão**:
  - Vela mestra: período de 5 minutos.
  - Vela seguidora: intervalo de tempo de 1 minuto.
  - Duração SMA: 20 períodos em ambos os fluxos.
  - Tolerância de sincronização: 15 segundos entre os tempos de abertura da vela.
- **Filtros**:
  - Categoria: Confirmação de tendência/multiperíodo.
  - Direção: Bidirecional.
  - Indicadores: SMA (fluxo duplo).
  - Paradas: Sem parada fixa, nivelamento automático quando os gráficos divergem.
  - Complexidade: Média (multi-assinatura com verificações de sincronização).
  - Prazo: Configurável (padrão intradiário).
  - Sazonalidade: Nenhuma.
  - Redes neurais: Não.
  - Divergência: Usa a divergência de prazo como filtro (requer acordo, não divergência de preço).
  - Nível de risco: Moderado devido à exigência de confirmação.

## Como funciona

1. Duas assinaturas de velas são criadas por meio do StockSharp API de alto nível: uma para o gráfico mestre e outra para o seguidor.
2. Cada feed é processado por um SMA com o mesmo comprimento, gerando um sinalizador de direção de tendência (`up` se o valor de SMA aumentar em relação ao
vela anterior, `down` caso contrário).
3. Sempre que ambas as velas terminam, a estratégia verifica se os seus timestamps estão suficientemente próximos (diferença absoluta abaixo do
tolerância configurada).
4. Se os gráficos estiverem sincronizados e ambas as tendências apontarem para cima, a estratégia compra (fechando primeiro qualquer posição vendida). Se ambas as tendências apontarem para baixo,
ele vende a descoberto (fechando qualquer posição comprada primeiro).
5. Qualquer perda de sincronização ou desacordo de tendência desencadeia um achatamento imediato para manter a conta alinhada com os gráficos.
relógios de comerciante.

## Uso recomendado

- Aplicar ao mesmo instrumento em dois intervalos de tempo diferentes que normalmente se correlacionam (por exemplo, 5 minutos e 1 minuto, ou horário e
15 minutos).
- Aumente a tolerância de sincronização se você trabalhar com fontes de dados exóticas que imprimem velas com pequenos atrasos.
- Combine com um gerenciador de risco externo ou módulo de parada complementar ao implantar para negociação ao vivo.
