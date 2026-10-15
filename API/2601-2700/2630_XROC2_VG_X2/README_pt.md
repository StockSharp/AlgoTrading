# Estratégia XROC2 VG X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia XROC2 VG X2 é um sistema multi-período que combina dois streams suavizados de taxa de variação. O período superior atua como filtro direcional enquanto o inferior produz sinais concretos de entrada e saída. O assessor especializado original do MetaTrader 5 dependia do indicador personalizado XROC2_VG com opções flexíveis de suavização e um módulo de gerenciamento de capital. O port do StockSharp mantém a lógica de sinais intacta e expõe os parâmetros-chave como entradas da estratégia.

A estratégia se subscreve a duas séries de velas:
- **Período superior** (padrão 6 horas) – estabelece a direção de tendência predominante.
- **Período inferior** (padrão 30 minutos) – gera entradas e saídas monitorando como as duas linhas ROC suavizadas se cruzam.

Ambos os streams compartilham o mesmo modo de cálculo de taxa de variação, mas usam configurações individuais de suavização. Por padrão a estratégia aplica médias móveis Jurik, imitando a versão MQL. Os tipos avançados de suavização não diretamente suportados pelo StockSharp (JurX, ParMA, T3, VIDYA, AMA com controle de fase) recorrem à implementação de média móvel mais próxima disponível.

## Lógica de trading
1. **Detecção de tendência (período superior)**
   - Calcular dois valores ROC suavizados usando os períodos e métodos de suavização configurados.
   - Avaliar o par de linhas na barra definida por `HigherSignalBar`. Se a linha rápida estiver acima da lenta, a tendência é altista; caso contrário, baixista. Uma leitura neutra mantém a tendência atual em zero e desabilita o trading.
2. **Geração de sinais (período inferior)**
   - Calcular o mesmo par de valores ROC suavizados no período inferior.
   - Observar a barra concluída mais recente (deslocamento `LowerSignalBar`) e a barra anterior. A combinação dessas duas barras determina se um cruzamento acabou de ocorrer.
   - Uma configuração comprada aparece quando o período superior é altista, a linha rápida cruzou abaixo da lenta (cruzamento descendente) e posições compradas estão habilitadas.
   - Uma configuração vendida aparece quando o período superior é baixista, a linha rápida cruzou acima da lenta (cruzamento ascendente) e posições vendidas estão habilitadas.
3. **Gestão de posições**
   - Fechar posições compradas quando o cruzamento no período inferior indica baixa (`CloseBuyOnLower`) ou quando a tendência do período superior muda para baixista (`CloseBuyOnTrendFlip`).
   - Fechar posições vendidas quando o cruzamento no período inferior se torna altista (`CloseSellOnLower`) ou quando a tendência do período superior muda para altista (`CloseSellOnTrendFlip`).
   - Novas operações são abertas apenas quando não há nenhuma posição ativa. O tamanho da ordem é controlado pela propriedade `Volume` da estratégia.

## Parâmetros
- `HigherCandleType` – tipo de vela para o filtro de tendência (padrão período de 6 horas).
- `LowerCandleType` – tipo de vela para geração de sinais (padrão período de 30 minutos).
- `HigherSignalBar` – quantas barras fechadas deslocar ao ler valores do período superior (padrão 1).
- `LowerSignalBar` – quantas barras fechadas deslocar ao ler valores do período inferior (padrão 1).
- `HigherRocMode` / `LowerRocMode` – variante de cálculo de taxa de variação (`Momentum`, `RateOfChange`, `RateOfChangePercent`, `RateOfChangeRatio`, `RateOfChangeRatioPercent`).
- `HigherFastPeriod`, `HigherFastMethod`, `HigherFastLength`, `HigherFastPhase` – configurações ROC rápido para o período superior.
- `HigherSlowPeriod`, `HigherSlowMethod`, `HigherSlowLength`, `HigherSlowPhase` – configurações ROC lento para o período superior.
- `LowerFastPeriod`, `LowerFastMethod`, `LowerFastLength`, `LowerFastPhase` – configurações ROC rápido para o período inferior.
- `LowerSlowPeriod`, `LowerSlowMethod`, `LowerSlowLength`, `LowerSlowPhase` – configurações ROC lento para o período inferior.
- `AllowBuyOpen`, `AllowSellOpen` – habilitar ou desabilitar abertura de posições compradas e vendidas.
- `CloseBuyOnTrendFlip`, `CloseSellOnTrendFlip` – forçar saídas quando o período superior muda de direção.
- `CloseBuyOnLower`, `CloseSellOnLower` – sair quando o cruzamento no período inferior vai contra a posição.

## Notas de implementação
- A estratégia MQL original usava uma grande biblioteca de suavização. A versão StockSharp mapeia as opções suportadas para indicadores integrados (SMA, EMA, SMMA/RMA, LWMA, Jurik, Kaufman AMA). Os modos não suportados (JurX, ParMA, T3, VIDYA) são aproximados com a média móvel mais próxima disponível, portanto o comportamento pode diferir para essas combinações.
- Funções de gerenciamento de capital, stop-loss, take-profit e configurações de deslizamento de `TradeAlgorithms.mqh` não são reproduzidas. Em vez disso, a estratégia opera com o `Volume` fixo especificado nas configurações da estratégia.
- As ordens são executadas com ordens de mercado. Lógica de proteção como stop-losses ou trailing stops pode ser adicionada via módulos de proteção do StockSharp se necessário.
- A estratégia só opera quando ambas as subscrições de velas estão completamente formadas e `IsFormedAndOnlineAndAllowTrading()` retorna verdadeiro.

## Dicas de uso
- Escolher tipos de velas que correspondam ao estilo de trading original (p. ex., 6h/30m para swing trading). Outras combinações são possíveis.
- Ajustar os períodos ROC e os métodos de suavização para corresponder à capacidade de resposta preferida. A suavização Jurik mantém o comportamento mais próximo do script fonte.
- Considerar adicionar gerenciamento de risco explícito (stop-loss, dimensionamento de posição) ao operar em contas reais, pois o port usa saídas de mercado simples.
