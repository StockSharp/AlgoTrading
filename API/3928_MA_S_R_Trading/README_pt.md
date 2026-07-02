# MA S. R. Estratégia de Negociação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O MA S.R. A estratégia de negociação é um sistema de reversão de tendência convertido do consultor MetaTrader original "MA SR Trading". Ele monitora a forma de uma média móvel simples curta (SMA) para detectar quando a dinâmica do preço se curva para um topo ou fundo local. Quando o SMA atinge picos ou vales, a estratégia entra imediatamente na direção da curva e protege a posição com um nível de stop ancorado na oscilação mais recente.

Ao contrário dos sistemas de cruzamento clássicos que comparam múltiplas médias móveis com comprimentos diferentes, esta abordagem analisa a curvatura do mesmo SMA comparando seu valor nas três velas concluídas mais recentes. Um máximo local (`SMA[t-2]` maior que `SMA[t-1]` e `SMA[t-3]`) sinaliza uma reversão de baixa e aciona uma entrada curta. Um mínimo local (`SMA[t-2]` abaixo de ambos os vizinhos) sinaliza uma reversão de alta e abre uma posição longa. Imediatamente após um sinal, a estratégia armazena o preço extremo em uma janela de lookback configurável e o utiliza como um stop de proteção.

A lógica de saída imita a modificação final da origem MQL. Para negociações curtas, o stop é definido para o máximo mais alto dentro da janela de lookback, desde que este nível permaneça acima do fechamento anterior (caso contrário, o nível será ignorado). As posições longas usam o mínimo mais baixo sob a mesma regra. Se o preço atingir o nível armazenado nas velas subsequentes, a estratégia fecha a posição no mercado, emulando efetivamente a atualização do stop loss do especialista original.

O sistema foi projetado para instrumentos que apresentam comportamento de oscilação pronunciado em gráficos intradiários e de curto prazo. Períodos curtos SMA (padrão = 5) permitem que o algoritmo reaja rapidamente às mudanças na microestrutura, enquanto o stop lookback (padrão = 5 barras para máximos e mínimos) controla a agressividade com que o nível final segue o preço. Use janelas mais estreitas para ambientes de scalping e configurações mais amplas para mercados mais barulhentos.

Os backtests sobre principais moedas e CFDs de índices líquidos mostram o melhor desempenho durante períodos variados com oscilações frequentes. Tendências com retrocessos suaves podem exigir filtros adicionais ou confirmação de volatilidade para evitar reversões prematuras. Considere combinar a estratégia com um contexto de mercado mais amplo ou filtros de tempo ao implantar ao vivo.

## Detalhes

- **Condições de entrada**
  - **Curto**: `SMA[t-1] < SMA[t-2]` E `SMA[t-3] < SMA[t-2]`. A última amostra SMA concluída forma um máximo local.
  - **Longo**: `SMA[t-1] > SMA[t-2]` E `SMA[t-3] > SMA[t-2]`. A última amostra SMA concluída forma um mínimo local.
- **Parar gerenciamento**
  - **Short**: Nível de stop = máxima mais alta dentro de `HighLookback` velas se o nível estiver acima do fechamento anterior. Sai quando o preço atinge o nível.
  - **Longo**: Nível de stop = mínimo mais baixo dentro de `LowLookback` velas se o nível estiver abaixo do fechamento anterior. Sai quando o preço atinge o nível.
- **Regras de Posição**: Sempre muda para o sinal mais recente. Ao reverter, a estratégia fecha a posição existente e abre a nova em uma única ordem de mercado dimensionada para cobrir a exposição anterior mais o volume desejado.
- **Parâmetros padrão**
  - `SmaPeriod` = 5.
  - `HighLookback` = 5.
  - `LowLookback` = 5.
  - `CandleType` = período de 30 minutos.
  - `TradeVolume` = 1 lote (aplicado na propriedade `Volume` no início).
- **Filtros**
  - Categoria: Reversão.
  - Direção: Longo e curto.
  - Indicadores: média móvel simples, rastreador de oscilação mais alta/mais baixa.
  - Paradas: Dinâmicas, baseadas em swing.
  - Prazo: intradiário para oscilação.
  - Complexidade: Média.
  - Nível de risco: Moderado (paradas apertadas, mas negociações frequentes).

## Notas de uso

1. Funciona melhor em instrumentos com oscilações visíveis. Considere desativar a negociação em torno de grandes eventos noticiosos para evitar falsas oscilações.
2. Otimize o período SMA e as janelas de lookback para o símbolo e período de tempo desejados. Configurações menores aumentam a sensibilidade, mas também as serras elétricas.
3. Os níveis de parada são recalculados somente quando um novo sinal de mudança de direção aparece. Se um stop se tornar inválido (por exemplo, um máximo que não esteja acima do fechamento anterior), ele será descartado, evitando que a estratégia coloque níveis de proteção muito próximos do preço.
4. Como as saídas dependem de ordens de mercado, a derrapagem pode ocorrer em movimentos rápidos. Combine com ordens de proteção do corretor se o local as apoiar.
5. A estratégia não utiliza metas de lucro. Para adicioná-los, estenda a lógica em `ProcessCandle` com condições adicionais.
