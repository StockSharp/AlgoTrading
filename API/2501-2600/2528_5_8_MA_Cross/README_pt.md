# Estratégia de Cruzamento de MA 5/8
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Cruzamento de MA 5/8 é um port do StockSharp do consultor especialista do MetaTrader "5_8 MACross". Compara uma média móvel exponencial (EMA) rápida calculada sobre preços de fechamento com uma EMA mais lenta calculada sobre preços de abertura. O sistema age no cruzamento entre as duas médias e pode ser aplicado a qualquer símbolo e período que forneça velas padrão baseadas em tempo.

## Indicadores
- **EMA rápida** – comprimento configurável (padrão 5) calculada a partir do preço de fechamento da vela.
- **EMA lenta** – comprimento configurável (padrão 8) calculada a partir do preço de abertura da vela.

## Lógica de trading
1. A estratégia processa apenas velas finalizadas para evitar dados parciais.
2. Uma entrada comprada é gerada quando a EMA rápida estava abaixo ou igual à EMA lenta na vela anterior e cruza acima dela na vela atual.
3. Uma entrada vendida é gerada quando a EMA rápida estava acima ou igual à EMA lenta na vela anterior e cruza abaixo dela na vela atual.
4. Quando um sinal aparece, a estratégia inverte sua exposição: fecha qualquer posição aberta e envia uma ordem a mercado dimensionada para terminar com contratos `Volume` na nova direção.

## Gestão de risco
- **Take profit** – alvo opcional expresso em pontos de preço. O tamanho do ponto é derivado do passo de preço do instrumento; para cotações de três e cinco dígitos o valor é automaticamente multiplicado por 10 para emular o tratamento de pips do MetaTrader.
- **Stop loss** – stop protetor opcional, também expresso em pontos de preço a partir do preço de entrada.
- **Trailing stop** – distância opcional em pontos de preço. Após uma posição ser aberta, a estratégia rastreia o maior máximo (para comprados) ou menor mínimo (para vendidos) e move o stop apenas na direção rentável. Se um stop loss inicial não for especificado, o trailing stop igualmente iniciará proteção imediatamente após a entrada.
- Se o take profit ou o (trailing) stop for atingido em um preço de fechamento, a posição é encerrada a mercado.

## Parâmetros
| Nome | Descrição | Valores padrão |
| --- | --- | --- |
| `FastLength` | Período da EMA rápida (baseada em fechamento). | 5 |
| `SlowLength` | Período da EMA lenta (baseada em abertura). | 8 |
| `TakeProfitPoints` | Distância do take profit em pontos de preço. | 40 |
| `StopLossPoints` | Distância do stop loss em pontos de preço (0 desabilita o stop). | 0 |
| `TrailingStopPoints` | Distância do trailing stop em pontos de preço (0 desabilita o trailing). | 0 |
| `CandleType` | Tipo/período de vela usado para os cálculos. | Período de 1 minuto |
| `Volume` | Volume de ordem herdado da classe base `Strategy`. | 0.1 |

## Diferenças comparadas à versão MQL
- Verificações de hedging específicas do MetaTrader e chamadas de informações de conta são omitidas porque o StockSharp lida com a contabilidade de posições de forma diferente.
- Os sinais são avaliados em velas fechadas em vez do primeiro tick de uma nova barra; isso melhora a estabilidade em ambientes orientados a eventos.
- A lógica de trailing usa o máximo/mínimo da vela para avançar o stop em vez do tick atual de bid/ask, fornecendo comportamento determinístico para processamento histórico.

## Notas de uso
- Configurar `Volume` nas propriedades da estratégia para corresponder ao tamanho de lote desejado.
- Combinar a estratégia com os módulos de proteção do StockSharp ou filtros adicionais se for necessário gerenciamento de risco em nível de portfólio.
- A estratégia não coloca ordens pendentes; todas as entradas e saídas são executadas com ordens a mercado geradas pelo cruzamento e lógica de risco acima.
