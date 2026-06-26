# Estratégia Probe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Probe reproduz o expert advisor do MetaTrader 5 "Probe" dentro do framework de alto nível do StockSharp. Monitora o Commodity Channel Index (CCI) em um período configurável e reage quando o oscilador rompe fora de um canal simétrico. Quando ocorre um rompimento, a estratégia coloca uma ordem stop com um deslocamento do preço de mercado atual baseado em pips. A abordagem busca capturar a continuação do momentum após o rompimento enquanto mantém o risco limitado por níveis protetores baseados em pips e um trailing stop adaptativo.

## Lógica de trading
1. Calcular o CCI no tipo de vela configurado.
2. Rastrear os valores anteriores e atuais do CCI para detectar quando o indicador sai do limite inferior ou superior do canal.
3. Quando o CCI cruza para cima através de `-CCI Channel`, enviar uma ordem stop de compra acima do último fechamento usando a distância `Indent (pips)`.
4. Quando o CCI cruza para baixo através de `+CCI Channel`, enviar uma ordem stop de venda abaixo do último fechamento usando o mesmo indent em pips.
5. Apenas uma ordem stop pendente pode permanecer ativa por vez. As ordens opostas são canceladas e novos sinais são ignorados enquanto uma ordem está ativa.

## Gerenciamento de ordens
- As ordens stop pendentes são retiradas se o mercado se afastar do preço de entrada em mais de `1.5 * Indent (pips)`. Isso espelha a lógica do MetaTrader que evita que ordens obsoletas permaneçam no livro quando o momentum desaparece.
- Uma vez que uma ordem stop é executada, a estratégia armazena o preço executado como a referência de entrada. Quaisquer ordens pendentes opostas são canceladas imediatamente.

## Gestão de risco
- Um stop-loss inicial é derivado de `Stop Loss (pips)` e anexado à posição ativa via monitoramento interno. Quando o preço toca o stop, a posição é encerrada com uma ordem de mercado.
- O comportamento de trailing começa depois que o lucro flutuante excede `Trailing Stop (pips) + Trailing Step (pips)`. O stop é então movido para assegurar lucros respeitando a distância mínima de trailing.
- Todas as distâncias baseadas em pips se ajustam automaticamente para cotações de 3 e 5 dígitos escalando o tamanho do tick da bolsa.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Período principal usado para construir velas e calcular o CCI. |
| `CciLength` | Período de média do oscilador CCI. |
| `CciChannelLevel` | Limiar absoluto do CCI que forma o canal de rompimento simétrico. |
| `IndentPips` | Distância em pips adicionada ao último fechamento ao colocar a ordem stop pendente. |
| `StopLossPips` | Distância do stop-loss protetor medida em pips. |
| `TrailingStopPips` | Limiar de lucro em pips necessário antes de o trailing stop ser ativado. |
| `TrailingStepPips` | Distância adicional de lucro necessária antes de o trailing stop ser movido novamente. |

## Notas
- Use a propriedade `Volume` da estratégia para controlar o tamanho negociado.
- A estratégia é projetada para netting de posição única, correspondendo ao comportamento do Expert Advisor original.
- A renderização do gráfico desenha velas, o indicador CCI e operações executadas quando uma área de gráfico está disponível.
