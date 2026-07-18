# Estratégia Volatility HFT EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista **Volatility HFT EA** do MetaTrader 5 para a API de alto nível do StockSharp. Reproduz a lógica original que compra quando o preço de fechamento salta bem acima de uma média móvil simples rápida e aguarda um pullback para essa média. A geração de ordens, o gerenciamento de indicadores e as saídas protetoras seguem as diretrizes de `AGENTS.md` enquanto mantêm o comportamento do script MQL.

## Como funciona

1. **Configuração do indicador** – uma única média móvil simples (comprimento padrão: 5) é calculada no período de trabalho especificado por `CandleType`.
2. **Detecção de nova barra** – o processamento ocorre apenas uma vez que uma vela está concluída (`CandleStates.Finished`), espelhando as verificações `IsNewBar` no EA.
3. **Requisito de aquecimento** – a estratégia aguarda 60 velas concluídas antes de avaliar trades, correspondendo ao guarda `Bars < 60` usado no MQL.
4. **Filtro de entrada** – uma configuração longa aparece quando o último fechamento está pelo menos `MaDifferencePips` acima da SMA (diferença convertida em preço usando o tamanho do pip do instrumento) e o valor da SMA é mais alto do que era duas barras atrás. O EA original usava `val[0] < -0.0015` e `MA_Val1[0] > MA_Val1[2]`; este port verifica as mesmas condições sem armazenar manualmente os arrays.
5. **Uma posição por vez** – apenas trades longos são suportados porque a ramificação de venda foi comentada no arquivo fonte. Um novo sinal é ignorado enquanto há uma posição aberta.

## Gestão de risco

- **Stop-loss** – stop de proteção opcional expresso em pips. O código deriva o tamanho do pip de `Security.PriceStep`, multiplicando por 10 quando o instrumento tem 3 ou 5 casas decimais, reproduzindo o escalonamento `_Digits` do MetaTrader.
- **Take-profit** – o alvo de saída está ancorado ao valor da SMA capturado na entrada, espelhando a chamada `mrequest.tp = MA_Val1[0];`. A estratégia fecha a posição quando a mínima da vela toca o nível SMA armazenado, emulando uma ordem limitada na média.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `OrderVolume` | Volume usado para cada ordem de mercado. |
| `FastMaLength` | Período da média móvil simples rápida (padrão 5). |
| `StopLossPips` | Distância do stop-loss em pips; definir como `0` para desabilitar. |
| `MaDifferencePips` | Distância mínima (em pips) entre o fechamento e a SMA necessária para uma entrada longa. |
| `CandleType` | Período usado para subscrição de velas e cálculos do indicador. |

`MinimumBars` é uma constante interna fixa igual a `60`, refletindo o requisito do EA para histórico suficiente.

## Uso

1. Vincule a estratégia a um ativo e selecione o `CandleType` desejado (por exemplo, barras de 1 minuto para comportamento de alta frequência).
2. Ajuste `FastMaLength`, `MaDifferencePips` e `StopLossPips` para se adequar à volatilidade do instrumento. As entradas baseadas em pip são automaticamente convertidas usando o tamanho de pip detectado, então os mesmos padrões funcionam em símbolos FX de 4 e 5 dígitos.
3. Configure `OrderVolume` para corresponder às suas regras de dimensionamento do portfólio. A estratégia envia apenas ordens de mercado e não avolumará posições.
4. Inicie a estratégia. Subscreverá as velas escolhidas, construirá a SMA, aguardará 60 barras de aquecimento e então avaliará entradas em cada vela concluída.
5. Monitore o gerenciamento de trades: as saídas são acionadas pelo toque da SMA ou pelo preço do stop-loss derivado na entrada.

## Notas e diferenças com o EA original

- A versão MQL solicitava o tamanho mínimo de lote via `SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN)`; aqui o volume é exposto como parâmetro para manter a estratégia flexível entre corretoras e classes de ativos.
- As condições de venda são omitidas porque estavam comentadas em `Volatility_HFT_EA.mq5`. O comportamento portanto corresponde ao script publicado, que apenas executava a ramificação longa.
- O gerenciamento de take-profit usa mínimas de velas para detectar um toque da média móvil em vez de registrar uma ordem limitada, garantindo que a mesma intenção funcione de forma confiável dentro do fluxo de trabalho do StockSharp.
- O gerenciamento manual de arrays (`CopyRates`, `CopyBuffer`, `ArraySetAsSeries`) é substituído por vínculos de indicadores do StockSharp. Isso reduz o código repetitivo enquanto preserva os limiares originais e as comparações de inclinação.
- Todos os cálculos permanecem baseados em velas; a estratégia não consulta buffers de indicadores com `GetValue`, mantendo-se em conformidade com as diretrizes do repositório.
