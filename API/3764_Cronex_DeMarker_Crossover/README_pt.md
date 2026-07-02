# Estratégia de cruzamento Cronex DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia cruzada Cronex DeMarker reproduz o indicador MetaTrader **Cronex DeMarker** e o transforma em um sistema de negociação automatizado. O indicador original representa graficamente o oscilador DeMarker junto com duas médias móveis lineares ponderadas (LWMAs). A estratégia reflete essa configuração, avalia cruzamentos de alta e baixa entre as linhas suavizadas do oscilador e os converte em ordens de mercado. Isso permite que a lógica de negociação reaja imediatamente quando o momentum muda de pressão negativa para pressão positiva (e vice-versa) de acordo com o indicador.

## Construção do indicador
1. **Oscilador DeMarker** – Mede a relação entre a vela atual e a vela anterior:
   - Se o máximo atual for superior ao máximo anterior, a pressão positiva é igual à diferença dos máximos; caso contrário, é zero.
   - Se o mínimo atual for inferior ao mínimo anterior, a pressão negativa será igual à distância entre os mínimos; caso contrário, é zero.
   - As somas das pressões positivas e negativas em `DeMarkerPeriod` barras formam o valor do oscilador `deMax / (deMax + deMin)`.
2. **LWMA rápido** – Uma média móvel ponderada linear com período `FastMaPeriod` é aplicada aos valores brutos do DeMarker para enfatizar as últimas alterações do oscilador.
3. **LWMA lento** – Outra média móvel ponderada linear com período `SlowMaPeriod` suaviza o mesmo fluxo DeMarker para construir uma linha de confirmação mais lenta.

A estratégia alimenta cada vela finalizada para esta pilha de indicadores, correspondendo exatamente aos cálculos do buffer do arquivo MQ4 original.

## Lógica de negociação
1. Aguarde até que o oscilador DeMarker e ambos os LWMAs estejam totalmente formados.
2. Após cada vela concluída, calcule o novo valor do DeMarker e atualize ambas as médias móveis.
3. Detecte cruzamentos entre as séries LWMA rápida e lenta:
   - **Cruzamento de alta** – O LWMA rápido se move de baixo para cima do LWMA lento. A estratégia fecha qualquer exposição curta e abre uma posição longa no mercado.
   - **Cruzamento de baixa** – O LWMA rápido se move de cima para baixo do LWMA lento. A estratégia fecha qualquer exposição longa e abre uma posição curta no mercado.
4. As ordens são ignoradas enquanto a estratégia ainda não está formada, enquanto está offline ou quando a negociação está desativada.

As posições são invertidas imediatamente em sinais opostos. A exposição existente é encerrada adicionando a quantidade necessária à nova ordem de mercado.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `DeMarkerPeriod` | Número de velas usadas para construir o oscilador DeMarker. | `25` |
| `FastMaPeriod` | Período da média móvel ponderada linear rápida que reage a novos valores do oscilador. | `14` |
| `SlowMaPeriod` | Período da média móvel ponderada linear lenta que confirma a direção. | `25` |
| `CandleType` | Série de velas processadas pela estratégia (período ou outro `DataType`). | `1 Hour` período de tempo |

## Detalhes de implementação
- Usa o `SubscribeCandles` API de alto nível. Os indicadores são atualizados somente quando uma vela atinge o estado `Finished` para evitar a repintura da barra intermediária.
- A estratégia depende dos indicadores `DeMarker` e `WeightedMovingAverage` integrados de StockSharp para replicar fielmente os buffers MQ4.
- Uma área do gráfico é criada automaticamente, traçando as velas de preço junto com o oscilador e ambas as médias móveis para confirmação visual.
- `StartProtection()` é invocado durante a inicialização para que a proteção de posição seja acionada exatamente uma vez, conforme exigido pelas diretrizes do projeto.

## Uso
1. Anexe a estratégia ao título desejado e atribua o tipo de vela preferido (por exemplo, velas com período de 1 hora).
2. Configure o DeMarker e os períodos de média móvel para corresponder ao indicador original ou ajuste-os para otimização.
3. Execute a estratégia. Ele começará a ser negociado assim que os indicadores estiverem totalmente formados e a negociação for permitida.
4. Monitore o gráfico plotado para ver o oscilador DeMarker e os sinais de cruzamento LWMA conduzindo as entradas.
