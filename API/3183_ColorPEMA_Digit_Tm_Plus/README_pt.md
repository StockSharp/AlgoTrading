# Estratégia de Exp Color PEMA Digit Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Exp Color PEMA Digit Tm Plus** é um port direto do consultor especialista do MetaTrader 5 "Exp_ColorPEMA_Digit_Tm_Plus". A estratégia reconstrói o indicador original de Média Móvel Exponencial Quíntupla (PEMA) e reproduz cada flag de permissão de trading presente no EA. As ordens são executadas na série de velas selecionada somente após o indicador confirmar uma mudança de cor e o período de espera opcional (`Signal Bar`) ter decorrido.

A versão StockSharp mantém as mesmas opções de gestão monetária, controles de stop/alvo e saída baseada em tempo que existiam na implementação MQL. Cada configuração é exposta através de `StrategyParam<T>` para suportar configuração de UI e otimização.

## Lógica do indicador
* O indicador alimenta uma cascata de oito médias móveis exponenciais usando o `PEMA Length` e o `Applied Price` configurados.
* A linha final é arredondada para os `Rounding Digits` solicitados, exatamente como no indicador original.
* A inclinação da linha arredondada produz três estados:
  * **Up (magenta)** – pressão de alta, possível configuração comprada.
  * **Flat (cinza)** – neutro, sem ação.
  * **Down (azul dodger)** – pressão de baixa, possível configuração vendida.
* A estratégia armazena o estado do indicador de cada vela concluída para poder referenciar barras mais antigas quando `Signal Bar` é maior que zero.

## Regras de trading
1. **Detecção de sinal** – em uma vela concluída, avaliar o estado do indicador que tem `Signal Bar` velas de idade e compará-lo com o estado anterior.
2. **Configuração comprada** – quando o estado muda para *Up* de qualquer outra coisa:
   * enfileirar uma entrada comprada se `Allow Long Entries` está habilitado;
   * enfileirar uma saída de vendidos existentes se `Allow Short Exits` está habilitado.
3. **Configuração vendida** – quando o estado muda para *Down* de qualquer outra coisa:
   * enfileirar uma entrada vendida se `Allow Short Entries` está habilitado;
   * enfileirar uma saída de comprados existentes se `Allow Long Exits` está habilitado.
4. **Camada de execução** – as ações enfileiradas são executadas somente quando:
   * a estratégia está online e o trading está permitido;
   * o timestamp de ativação vinculado à vela fonte foi atingido; e
   * as regras de dimensionamento de posição permitem um volume não nulo.
5. **Gestão de risco** –
   * os níveis opcionais de stop-loss e take-profit são derivados do preço de execução usando as mesmas distâncias em pontos do MetaTrader;
   * `Use Time Exit` fecha posições que excedam o tempo de vida configurado em `Holding Minutes`;
   * os sinais opostos podem imediatamente zerar a exposição se a permissão de saída respectiva estiver ativa.

## Parâmetros
| Nome | Descrição |
| ---- | --------- |
| Money Management | Valor base usado pelas regras de dimensionamento de posição. |
| Money Mode | Escolhe entre dimensionamento baseado em lotes ou modelos de percentual de saldo/margem livre. |
| Stop Loss (points) | Distância ao stop loss em pontos de preço. |
| Take Profit (points) | Distância ao take profit em pontos de preço. |
| Allowed Deviation | Parâmetro de marcador preservado do EA por completude. |
| Allow Long Entries / Allow Short Entries | Habilitar ou desabilitar a abertura de operações em cada direção. |
| Allow Long Exits / Allow Short Exits | Habilitar ou desabilitar o fechamento de operações quando sinais opostos aparecem. |
| Use Time Exit | Ativa a lógica de zeragem baseada em tempo. |
| Holding Minutes | Tempo máximo de manutenção de uma posição, expresso em minutos. |
| Candle Type | Série de velas processada pela estratégia. Padrão H4. |
| PEMA Length | Comprimento usado para todos os oito estágios EMA na PEMA Quíntupla. |
| Applied Price | Preço fonte usado no cálculo do indicador. |
| Rounding Digits | Dígitos decimais usados para arredondar a saída do indicador. |
| Signal Bar | Número de barras concluídas a aguardar antes de avaliar um sinal. |

## Notas de uso
* Colocar a estratégia dentro de um conector StockSharp que forneça acesso ao instrumento desejado e à série de velas.
* Configurar os parâmetros para corresponder ao setup do MetaTrader que deseja replicar.
* Executar backtests ou trading ao vivo conforme necessário; a estratégia reage apenas a velas completamente fechadas.

## Status de conversão
* **Versão C#** – implementada (`CS/ExpColorPemaDigitTmPlusStrategy.cs`).
* **Versão Python** – não criada (conforme instrução).
