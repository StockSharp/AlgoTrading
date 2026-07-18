# Estratégia de Contratendência XFatl XSatl Cloud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia do StockSharp recria o expert MT5 **Exp_XFatlXSatlCloud**. Ela observa a "nuvem" FATL/SATL suavizada e opera **contra** a direção de seu cruzamento. Quando a linha rápida (XFATL) cai abaixo da linha lenta (XSATL) depois de ter estado acima, a estratégia abre uma posição comprada. Quando a linha rápida sobe acima depois de ter estado abaixo, ela abre uma posição vendida. Níveis opcionais de stop loss e take profit são expressos em passos de preço do instrumento.

## Lógica de trading

- A fonte de dados padrão é um timeframe de 8 horas. Outros tipos de candles podem ser selecionados com o parâmetro `CandleType`.
- Dois pipelines de suavização são construídos a partir de médias móveis do StockSharp. Por padrão ambos usam uma média móvel Jurik com comprimento e fase configuráveis. Famílias de suavização alternativas (SMA, EMA, SMMA, WMA) também estão disponíveis.
- Os sinais são avaliados na barra definida por `SignalBar` (deslocamento em barras a partir do último candle fechado). A estratégia armazena uma janela deslizante de valores recentes do indicador para que os últimos e anteriores valores possam ser comparados assim como a versão MT5.
- Regras de entrada (contrária):
  - **Comprado** – a linha rápida estava acima da linha lenta na barra anterior e agora cruzou para ou abaixo dela.
  - **Vendido** – a linha rápida estava abaixo na barra anterior e agora cruzou para ou acima dela.
- Regras de saída:
  - Posições compradas fecham quando a barra anterior mostrou uma nuvem baixista (rápida abaixo de lenta) e `AllowLongExit` está habilitado.
  - Posições vendidas fecham quando a barra anterior mostrou uma nuvem altista (rápida acima de lenta) e `AllowShortExit` está habilitado.
- Uma nova posição só é aberta uma vez que a posição anterior foi completamente fechada, espelhando o comportamento do consultor especialista original.

## Gestão de risco

- `TradeVolume` controla a quantidade usada para ordens de mercado. A estratégia nunca escala — cada nova posição usa o mesmo tamanho.
- `TakeProfitTicks` e `StopLossTicks` se convertem diretamente em distâncias de passo de preço e são conectados ao módulo de proteção integrado do StockSharp. Configure-os como zero para desabilitar a ordem protetora correspondente.
- Como o expert MT5 dependia de cálculos de gerenciamento de dinheiro específicos do broker, esta versão substitui essa lógica por parâmetros explícitos de volume e proteção.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Tipo de candle ou timeframe usado para cálculos do indicador. |
| `FastMethod` / `SlowMethod` | Família de suavização para XFATL e XSATL (Jurik por padrão). |
| `FastLength` / `SlowLength` | Comprimentos de período para os filtros rápido e lento. |
| `FastPhase` / `SlowPhase` | Entradas de fase encaminhadas para a média móvel Jurik quando suportado. |
| `SignalBar` | Deslocamento de barra usado ao avaliar cruzamentos (1 = barra anterior). |
| `TradeVolume` | Tamanho de ordem para entradas. |
| `AllowLongEntry` / `AllowShortEntry` | Habilitar ou desabilitar entradas contrárias em cada direção. |
| `AllowLongExit` / `AllowShortExit` | Permitir que o indicador feche posições abertas em sinais opostos. |
| `TakeProfitTicks` | Distância ao alvo de take-profit expressa em passos de preço. |
| `StopLossTicks` | Distância ao stop protetor em passos de preço. |

## Notas de implementação

- A estratégia mantém filas curtas de saídas recentes do indicador e as apara ao comprimento mínimo exigido por `SignalBar`. Nenhum buffer histórico adicional é criado.
- O suporte à fase Jurik é configurado via reflexão para que a estratégia permaneça compatível com diferentes versões do StockSharp. Se o indicador subjacente não tiver uma propriedade `Phase`, o valor é simplesmente ignorado.
- Apenas o preço de fechamento de cada candle é usado, correspondendo à configuração mais comum para o expert original. Estender a lógica para tipos de preço alternativos exigiria aumentar a estratégia.
- Componentes de API de alto nível (`SubscribeCandles`, `Bind`, `StartProtection`) são usados ao longo, portanto a estratégia se integra perfeitamente ao Designer e outros produtos StockSharp.
