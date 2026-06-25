# Estratégia de Oscilação MACD 1H EUR/USD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o assessor especializado MetaTrader "1H EUR_USD" para a API de alto nível do StockSharp. Ela opera o par EUR/USD em velas horárias usando médias móveis duplas e detecção de oscilações MACD. As entradas requerem tanto um filtro de tendência (MA rápida acima/abaixo da MA lenta) quanto um padrão de fundo duplo/topo duplo MACD combinado com um rompimento de máximos ou mínimos recentes. O risco é controlado com stop loss, take profit baseados em pips e um trailing stop incremental que espelha a lógica do EA original.

## Detalhes

- **Mercado**: Projetado para EUR/USD no período de 1 hora, mas pode ser aplicado a qualquer instrumento que produza velas padrão.
- **Critérios de entrada**:
  - **Comprado**:
    - A MA rápida está acima da MA lenta (tipo selecionável entre SMA, EMA, SMMA, LWMA).
    - A linha principal MACD forma qualquer uma das seguintes oscilações altistas completamente abaixo da linha zero:
      - `MACD[-1] > MACD[-2] < MACD[-3]` com `MACD[-2] < 0` e o fechamento atual rompe o máximo da vela anterior.
      - `MACD[-2] > MACD[-3] < MACD[-4]` com `MACD[-3] < 0` e o fechamento atual rompe o máximo de duas velas atrás.
  - **Vendido**:
    - A MA rápida está abaixo da MA lenta.
    - A linha principal MACD forma as oscilações baixistas espelhadas completamente acima da linha zero e o preço fecha abaixo do mínimo anterior relevante.
- **Critérios de saída**:
  - Take profit e stop loss baseados em pips são anexados imediatamente após a entrada.
  - O trailing stop se ativa apenas após o preço se mover favoravelmente por `TrailingStop + TrailingStep` pips e então segue o preço a uma distância de `TrailingStop` pips, seguindo a lógica de modificação gradual do EA.
  - Ordens de proteção se ativam no máximo/mínimo intraperiodo da vela.
- **Gestão de posição**:
  - Usa o volume de operação configurado; reverter posições fecha o lado oposto antes de abrir o novo.
  - As operações compradas e vendidas compartilham os mesmos cálculos de pip (o tamanho do pip se adapta automaticamente a cotações de 4/5 dígitos).
- **Indicadores**:
  - Médias móveis rápida e lenta com tipo selecionável (Simples, Exponencial, Suavizado, Ponderado Linear) e deslocamento horizontal opcional.
  - MACD clássico (comprimentos de EMA rápida/lenta/sinal).
- **Parâmetros**:
  - `TradeVolume` – tamanho de lote base enviado com cada ordem.
  - `StopLossPips`, `TakeProfitPips` – distâncias de proteção em pips (defina como zero para desabilitar).
  - `TrailingStopPips`, `TrailingStepPips` – configuração de rastreamento; o passo de rastreamento deve permanecer positivo quando o rastreamento está ativo.
  - `FastMaLength`, `FastMaShift`, `FastMaType` – configurações da MA rápida.
  - `SlowMaLength`, `SlowMaShift`, `SlowMaType` – configurações da MA lenta.
  - `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – parâmetros MACD.
  - `CandleType` – período para processamento (padrão de 1 hora).
  - `LookbackPeriod` – preservado por compatibilidade com as entradas MQL; não altera a lógica porque o EA original também o deixou sem uso.

## Notas

- O comportamento do trailing stop espelha a versão MQL: nenhum ajuste ocorre até que tanto a distância de rastreamento quanto o passo de rastreamento sejam superados pelo lucro não realizado.
- A estratégia assume que o passo de preço é igual ao ponto de cotação; se o instrumento tem 3 ou 5 dígitos decimais, o código escala automaticamente o tamanho do pip por 10.
- Os comentários dentro do fonte C# explicam cada bloco-chave em inglês para facilitar a manutenção e extensão.
