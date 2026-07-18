# Estratégia XDeMarker Histogram Vol Direct
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o expert do MetaTrader 5 **Exp_XDeMarker_Histogram_Vol_Direct** usando a API de alto nível do StockSharp. Ela multiplica o oscilador XDeMarker pelo fluxo de volume escolhido, suaviza tanto o oscilador quanto o volume com o mesmo promedio móvel, e compara o resultado com níveis superiores/inferiores configuráveis. As decisões de negociação são tomadas quando o histograma suavizado muda de direção entre barras consecutivas.

## Lógica do indicador

1. Calcular o oscilador XDeMarker clássico no período selecionado.
2. Escalar o oscilador pela contagem de ticks ou volume real para cada candle finalizado.
3. Suavizar tanto o histograma quanto o volume com o tipo de média móvel selecionado.
4. Multiplicar o volume suavizado pelos multiplicadores de nível configurados para obter quatro bandas dinâmicas.
5. Detectar a direção do histograma (subindo ou caindo). Quando a direção muda, a estratégia abre uma nova posição na direção correspondente enquanto também fecha qualquer operação oposta.

O método de suavização suporta médias móveis simples, exponenciais, suavizadas (RMA/SMMA) e ponderadas. Os filtros exóticos da biblioteca original (JJMA, JurX, ParMA, T3, VIDYA, AMA) não estão disponíveis neste port.

## Regras de negociação

- **Entrada comprada** — habilitada quando `Allow Long Entry = true`. Se a barra anterior tinha direção "para cima" e a última barra mudou para "para baixo", a estratégia mira uma posição comprada de `Volume` lotes.
- **Entrada vendida** — habilitada quando `Allow Short Entry = true`. Acionada quando a barra anterior estava "para baixo" e a barra mais recente gira "para cima".
- **Saída comprada** — habilitada quando `Allow Long Exit = true`. Se a direção da barra anterior é "para baixo", a posição é liquidada, a menos que uma nova entrada vendida seja disparada na mesma barra.
- **Saída vendida** — habilitada quando `Allow Short Exit = true`. Ativada quando a direção da barra anterior é "para cima".

Os sinais são avaliados uma vez por candle finalizado. A implementação do StockSharp mantém o atraso original de uma barra; o parâmetro `Signal Bar` está presente como referência, mas valores diferentes de `1` são ignorados com um aviso.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| Candle Type | Período usado para construir candles para o indicador. |
| DeMarker Period | Período de retrospectiva para o oscilador XDeMarker base. |
| Volume Source | Escolher entre contagem de ticks e volume real negociado. |
| High Level 2 / High Level 1 | Multiplicadores aplicados ao volume suavizado para formar bandas superiores. |
| Low Level 1 / Low Level 2 | Multiplicadores para bandas inferiores. |
| Smoothing Method | Tipo de média móvel aplicado tanto ao histograma quanto ao volume. |
| Smoothing Length | Comprimento da janela de suavização. |
| Smoothing Phase | Marcador de posição de compatibilidade (não usado, mas mantido para paridade). |
| Signal Bar | Deslocamento histórico, fixo em 1 como no expert. |
| Allow Long/Short Entry | Habilitar abertura de posições na direção respectiva. |
| Allow Long/Short Exit | Habilitar fechamento automático de operações existentes. |

## Notas de implementação

- A classe `XDeMarkerHistogramVolDirectIndicator` reproduz os buffers do indicador MT5 e expõe o histograma suavizado, as bandas e os flags de direção através de um valor de indicador complexo.
- Quando uma nova exposição alvo é necessária, a estratégia envia uma única ordem de mercado que move a posição atual para o nível desejado (`Volume`, `-Volume` ou flat). Isso imita as chamadas sequenciais de fechamento/abertura no código MQL5 original sem duplicar ordens.
- O renderizador de gráfico traça automaticamente os candles, o indicador personalizado e as operações executadas quando uma área de gráfico está disponível.
