# Ingrit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Ingrit é uma conversão do consultor especialista de MetaTrader 5 `Ingrit.mq5`. A estratégia observa velas de cinco minutos e reage quando uma vela forte contra a tendência é seguida por um amplo rompimento medido contra um swing de quatorze barras atrás. As ordens são colocadas a mercado com distâncias configuráveis de stop-loss, take-profit e trailing stop expressas em pips. Os sinais podem ser opcionalmente invertidos ou forçados a achatar a exposição oposta antes de entrar em uma nova operação.

## Lógica da estratégia
### Detecção de rompimento
* A estratégia processa apenas velas finalizadas do período selecionado (padrão: 5 minutos).
* Para uma configuração **long**, a vela anterior deve fechar em baixa e a distância entre a máxima da vela 14 barras atrás e a mínima da vela anterior deve superar `StepPips` (após converter pips para unidades de preço).
* Para uma configuração **short**, a vela anterior deve fechar em alta e a distância entre a máxima da vela anterior e a mínima da vela 14 barras atrás deve superar `StepPips`.
* Habilitar `ReverseSignals` troca as condições long e short, recriando o modo de reversão opcional do robô original.

### Gestão de operações
* As ordens de mercado são enviadas usando o `Volume` da estratégia. Quando `CloseOppositePositions` está habilitado, o tamanho solicitado é aumentado pelo valor absoluto da posição atual para que as reversões fechem a exposição existente na mesma operação.
* Um stop-loss fixo e take-profit (se maior que zero) são anexados imediatamente após a entrada. Ambas as distâncias são convertidas de pips usando o passo de preço do instrumento e se adaptam automaticamente a cotações FX de três e cinco dígitos.
* O trailing stop fica ativo quando o lucro não realizado supera `TrailingStopPips + TrailingStepPips`. Para posições long o stop segue abaixo do fechamento; para posições short segue acima do fechamento. Cada atualização mantém o stop pelo menos `TrailingStepPips` afastado do nível de trailing anterior para evitar modificações rápidas.

### Comportamento adicional
* O trailing pode ser desativado definindo `TrailingStopPips` como zero. Se o trailing estiver ativo, o passo deve permanecer positivo (a estratégia realiza a mesma validação que a versão MQL).
* Todos os cálculos são executados em velas completadas; nenhum processamento intrabar é necessário no StockSharp.
* A estratégia não cria ordens pendentes: cada sinal é executado com uma ordem de mercado e os níveis de proteção são simulados internamente.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período utilizado para construir velas para a lógica de rompimento. Padrão: período de 5 minutos. |
| `StopLossPips` | Distância do stop-loss em pips. Um valor de `0` desabilita o stop fixo. |
| `TakeProfitPips` | Distância do take-profit em pips. Um valor de `0` desabilita o alvo fixo. |
| `TrailingStopPips` | Distância base do trailing stop em pips. Definir como `0` para desabilitar o trailing. |
| `TrailingStepPips` | Distância extra em pips que deve ser obtida antes que o trailing stop se mova novamente. Deve ser positivo quando o trailing está habilitado. |
| `StepPips` | Distância mínima de swing em pips entre a vela de referência e a última vela antes de um sinal ser acionado. |
| `ReverseSignals` | Se `true`, troca as condições long e short (modo de reversão). |
| `CloseOppositePositions` | Se `true`, amplia a ordem de mercado para achatar qualquer exposição oposta antes de abrir a nova posição. |
| `Volume` | Propriedade da estratégia que define o tamanho base da ordem. Combinar com `CloseOppositePositions` para controlar o comportamento de reversão. |

## Notas
* Os valores de pip são derivados do passo de preço do instrumento. Quando o instrumento usa três ou cinco casas decimais, a estratégia multiplica o passo por dez para que um pip seja igual à definição padrão de FX.
* Não há versão em Python para esta estratégia no repositório.
