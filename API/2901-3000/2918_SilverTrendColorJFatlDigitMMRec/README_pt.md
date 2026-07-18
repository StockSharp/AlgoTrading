# Estratégia SilverTrend ColorJFatl Digit MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma portagem StockSharp do consultor especializado MetaTrader `Exp_SilverTrend_ColorJFatl_Digit_MMRec`. Recria a arquitetura de bloco duplo onde dois módulos de lógica independentes gerenciam seus próprios tamanhos de posição virtuais e os combinam na posição final da estratégia:

- **Bloco SilverTrend** – lê as cores das velas produzidas pelo indicador SilverTrend para detectar quando o preço cruza as bordas do canal adaptativo.
- **Bloco ColorJFatl** – calcula uma FATL (Fast Adaptive Trend Line) filtrada usando a tabela de pesos publicada e um suavizador baseado em EMA que emula a média móvel Jurik usada no MetaTrader.

Ambos os módulos podem abrir operações compradas e vendidas de forma independente, fechar exposição oposta em novos sinais e aplicar suas próprias distâncias de stop-loss e take-profit. A posição final da estratégia é igual à soma das posições virtuais gerenciadas pelos dois blocos.

## Configuração padrão

- Símbolo: o instrumento da estratégia selecionado no StockSharp.
- Períodos: ambos os módulos usam velas de 6 horas por padrão (configuráveis através de parâmetros).
- Tamanho de ordem: cada módulo envia ordens de mercado com um parâmetro de volume separado (padrão `1`).

## Indicadores e lógica de sinal

### Bloco SilverTrend

1. Constrói um canal de preço deslizante das últimas `SSP` velas.
2. Aplica o deslocamento `Risk` original `(33 - Risk) / 100` para mover as bordas do canal dentro do intervalo máximo/mínimo.
3. Colore cada vela de acordo com a tendência ativa (`0`/`1` altista, `3`/`4` baixista, `2` neutro) como o indicador MetaTrader.
4. Sinais:
   - **Comprado** quando a vela na `Signal Bar` configurada torna-se altista enquanto a barra anterior não era (`color < 2` e anterior `> 1`).
   - **Vendido** quando se torna baixista enquanto a barra anterior não era (`color > 2` e anterior `< 3`).
5. Níveis opcionais de stop-loss e take-profit são medidos em pontos usando o passo de preço do instrumento.

### Bloco ColorJFatl

1. Constrói um valor FATL aplicando a tabela de coeficientes oficial à fonte de `Applied Price` escolhida.
2. Suaviza o resultado com uma EMA de comprimento `JMA Length` (o input de fase Jurik é preservado para compatibilidade e documentação).
3. Colore a linha FATL de acordo com a inclinação: `2` para subindo, `0` para descendo e `1` para segmentos planos.
4. Sinais:
   - **Comprado** quando a cor FATL muda para `2` enquanto a cor anterior era `0` ou `1`.
   - **Vendido** quando a cor muda para `0` enquanto o valor anterior era `1` ou `2`.
5. Cada direção pode opcionalmente fechar a posição do bloco oposto antes de abrir uma nova operação.

## Gerenciamento de risco

- SilverTrend e ColorJFatl cada um mantém seu próprio preço de entrada e distâncias de stop/alvo.
- Se um stop ou alvo for atingido, apenas o bloco afetado fecha sua posição virtual (o outro bloco pode permanecer aberto).
- Quando ambos os blocos concordam na mesma direção, seus volumes se acumulam.

## Parâmetros

| Grupo | Nome | Descrição |
| --- | --- | --- |
| SilverTrend | `Silver Candle Type` | Assinatura de velas usada para o indicador SilverTrend. |
| SilverTrend | `SSP` | Comprimento do intervalo máximo/mínimo deslizante. |
| SilverTrend | `Risk` | Fator de contração do canal (input `Risk` original). |
| SilverTrend | `Signal Bar` | Deslocamento de barra usado para o sinal (0 = barra fechada atual, 1 = barra anterior, etc.). |
| SilverTrend | `Allow Silver Long/Short` | Habilitar entradas para cada direção. |
| SilverTrend | `Close Silver Long/Short` | Permitir fechamento automático da posição oposta. |
| SilverTrend | `Silver Volume` | Volume para operações abertas pelo bloco SilverTrend. |
| SilverTrend | `Silver SL/TP` | Distâncias de stop-loss e take-profit em pontos. |
| ColorJFatl | `Color Candle Type` | Assinatura de velas usada para os cálculos FATL. |
| ColorJFatl | `JMA Length` | Comprimento do suavizador EMA que emula JMA. |
| ColorJFatl | `JMA Phase` | Preservado para completude (sem influência direta dentro do StockSharp). |
| ColorJFatl | `Applied Price` | Preço fonte (fechamento, mediano, típico, seguidor de tendência, etc.). |
| ColorJFatl | `Digits` | Precisão decimal aplicada ao valor FATL. |
| ColorJFatl | `Color Signal Bar` | Deslocamento de barra usado para sinais FATL. |
| ColorJFatl | Alternadores `Allow/Close` | Habilitar entradas e saídas automáticas para cada direção. |
| ColorJFatl | `Color Volume` | Volume para operações abertas pelo bloco ColorJFatl. |
| ColorJFatl | `Color SL/TP` | Distâncias de stop-loss e take-profit em pontos para o bloco. |

## Notas

- A estratégia assina ambos os fluxos de velas mesmo que sejam idênticos. Assinaturas duplicadas são tratadas internamente pelo StockSharp.
- O parâmetro de fase Jurik é mantido para ficar próximo ao consultor especializado original. O suavizador baseado em EMA do StockSharp replica o comportamento curvo da FATL enquanto mantém o parâmetro disponível para extensões futuras.
- Certifique-se de que o instrumento tem `PriceStep` definido para usar limites de risco baseados em pontos.

## Dicas de uso

1. Definir a propriedade `Volume` da estratégia ou ajustar os parâmetros de volume específicos do bloco para controlar a exposição absoluta.
2. Usar os sinalizadores de habilitar/desabilitar para testar cada bloco separadamente antes de combiná-los.
3. Como os blocos operam de forma independente, a estratégia pode manter um líquido comprado e vendido simultaneamente (por exemplo, comprado de SilverTrend e vendido de ColorJFatl) – a posição resultante é a soma algébrica de ambos.
4. Otimizar `SSP`, `Risk` e `JMA Length` para o mercado-alvo se planeja usar busca de parâmetros automatizada.
