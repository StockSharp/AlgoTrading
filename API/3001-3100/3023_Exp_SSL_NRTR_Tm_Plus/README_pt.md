# Estratégia Exp SSL NRTR Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia replica o consultor especialista MetaTrader "Exp_SSL_NRTR_Tm_Plus" usando a infraestrutura de alto nível do StockSharp. Ela
assina um único período, calcula o canal SSL NRTR com um método de suavização configurável e reage às transições
de cor fornecidas pelo indicador. As entradas compradas são acionadas quando o canal se torna altista enquanto as entradas vendidas ocorrem em
transições baixistas. A implementação preserva os controles de risco originais, os filtros de operações opcionais e a saída baseada em temporizador.

## Parâmetros

| Grupo | Parâmetro | Descrição |
| --- | --- | --- |
| Trading | Money Management | Fração do portfólio (ou lotes diretos quando negativo/modo `Lot`) usado para dimensionar ordens. |
| Trading | Margin Mode | Modo usado para traduzir o valor de gestão de dinheiro em um tamanho de posição. Modos diferentes de `Lot` são aproximados com cálculos baseados em portfólio. |
| Trading | Allow Long/Short Entries | Habilitar ou desabilitar a abertura de posições na direção respectiva. |
| Trading | Allow Long/Short Exits | Permitir que a estratégia feche posições na direção respectiva em reversões do indicador. |
| Risk | Stop Loss | Distância de stop de proteção em passos de preço. A estratégia monitora os níveis em vez de colocar ordens de stop nativas. |
| Risk | Take Profit | Distância de take profit em passos de preço. |
| Risk | Slippage | Parâmetro informacional mantido do EA original. |
| Risk | Use Time Exit | Habilitar o temporizador que força uma posição plana após o período de manutenção configurado. |
| Risk | Exit Minutes | Período de manutenção em minutos para a saída baseada em tempo. |
| Data | Candle Type | Período de trabalho usado tanto para negociação quanto para cálculos do indicador. |
| Indicator | Smoothing Method | Tipo de média móvel usado pelo canal SSL NRTR. Tipos personalizados não suportados recorrem a uma EMA. |
| Indicator | Length | Período base do algoritmo de suavização. |
| Indicator | Phase | Parâmetro auxiliar usado por médias adaptativas (T3, VIDYA, AMA). |
| Indicator | Signal Bar | Número de barras fechadas para olhar para trás ao avaliar as cores SSL. |

## Lógica de negociação

1. Assinar o período configurado e processar apenas velas terminadas.
2. Calcular as médias móveis SSL NRTR e derivar a cor do canal (para cima, para baixo ou neutro).
3. Quando a cor muda para altista (`0`), opcionalmente fechar posições vendidas e, se habilitado, abrir uma posição comprada.
4. Quando a cor muda para baixista (`2`), opcionalmente fechar posições compradas e, se habilitado, abrir uma posição vendida.
5. Rastrear os níveis de stop-loss/take-profit usando o preço de entrada e fechar a posição quando qualquer nível for atingido.
6. Opcionalmente fechar posições assim que o tempo de manutenção exceder o parâmetro `Exit Minutes`.
7. Prevenir entradas repetidas dentro da mesma barra com a lógica do "nível de tempo" original do MT5.

## Gestão de dinheiro

- O modo `Lot` trata o valor de gestão de dinheiro como um volume direto expresso em lotes/contratos.
- `FreeMargin` e `Balance` aproximam a fração de capital solicitada dividindo-a pelo último preço de fechamento.
- `LossFreeMargin` e `LossBalance` estimam o volume negociável a partir da perda permitida por operação usando a distância de stop-loss configurada.
- Valores negativos de gestão de dinheiro sempre mapeiam para um tamanho de lote absoluto.

## Notas

- Apenas os métodos de suavização disponíveis no StockSharp são implementados diretamente. `Jurx` e `Parma` recorrem à média móvel exponencial e esse comportamento está documentado nos comentários do código.
- A estratégia mantém a lógica de stop-loss e take-profit dentro do loop da estratégia em vez de enviar ordens de proteção nativas para permanecer agnóstica à plataforma.
- O deslizamento é uma configuração informacional mantida para completude; as ordens são enviadas como ordens de mercado simples.
- A implementação desenha velas e operações próprias na área do gráfico por padrão.
