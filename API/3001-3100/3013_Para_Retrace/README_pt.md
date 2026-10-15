# Estratégia de Retração Para
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia de Retração Para** é uma conversão em C# do consultor especialista original do MetaTrader 4 `Para_Retrace.mq4`. Ela reproduz a ideia de usar o indicador Parabolic SAR como âncora dinâmica e aguardar retrações do preço de volta a esse nível antes de entrar no mercado. A conversão aproveita a API de alto nível do StockSharp para gerenciar assinaturas de dados de mercado, atualizações de indicadores e execução de ordens.

## Lógica de Trading
1. Calcular o valor do Parabolic SAR em cada vela finalizada usando o passo de aceleração e a aceleração máxima configurados.
2. Determinar a tendência predominante verificando se toda a vela está abaixo ou acima do valor SAR:
   - **Contexto baixista:** se tanto o máximo quanto o mínimo da vela estiverem abaixo do valor SAR.
   - **Contexto altista:** caso contrário (o preço está tocando ou acima do nível SAR).
3. Derivar um preço gatilho deslocando o valor SAR por um número de pips definido pelo usuário:
   - Em um contexto baixista, a estratégia subtrai o deslocamento, aguardando uma retração para cima.
   - Em um contexto altista, a estratégia adiciona o deslocamento, aguardando um recuo para baixo.
4. Uma vez que o preço toca o gatilho (máximo cruza acima para vendidos, mínimo cruza abaixo para comprados), a estratégia abre uma ordem de mercado na direção da tendência.
5. Ordens de stop-loss e take-profit de proteção são anexadas automaticamente usando a facilidade `StartProtection` do StockSharp, correspondendo às distâncias do script original.

Ao contrário do consultor especialista original, a versão do StockSharp continua operando após a abertura de uma posição; não é necessário redefinir manualmente o valor de deslocamento. Todas as ações são realizadas apenas em velas concluídas para evitar problemas de repintura intrabarra.

## Indicadores
- **Parabolic SAR** – impulsiona tanto a detecção de tendência quanto os níveis de entrada.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `SarStep` | Fator de aceleração inicial para o Parabolic SAR. | `0.01` |
| `SarMax` | Fator de aceleração máximo para o Parabolic SAR. | `0.2` |
| `RetraceOffsetPips` | Distância (em pips) entre o valor SAR e o gatilho de entrada. | `30` |
| `StopLossPips` | Distância de stop-loss em pips (convertida para preço absoluto). Defina como `0` para desabilitar. | `30` |
| `TakeProfitPips` | Distância de take-profit em pips (convertida para preço absoluto). Defina como `0` para desabilitar. | `30` |
| `CandleType` | Período usado para velas e cálculos de indicadores. | `5 Minute` |

> **Nota:** A estratégia estima o tamanho do pip a partir dos metadados do título. Se o instrumento usar cinco casas decimais (típico para Forex), um pip equivale a dez passos mínimos de preço.

## Gerenciamento de Ordens
- As ordens são colocadas a mercado assim que a condição de retração é satisfeita.
- O tamanho de operação padrão é um lote (`Volume = 1`), mas pode ser ajustado pela propriedade base `Strategy.Volume` antes de iniciar a estratégia.
- `StartProtection` gerencia automaticamente as colocações de stop-loss e take-profit usando deslocamentos de preço absolutos derivados das configurações de pips.

## Dicas de Uso
- Ajuste o deslocamento de pips, o stop e o alvo para corresponder à volatilidade do instrumento sendo operado.
- Considere combinar a estratégia com filtros adicionais (hora do dia, volatilidade, etc.) ao integrar em um framework de trading mais amplo.
- Sempre realize backtests antes de implantar ao vivo, pois a lucratividade depende fortemente das condições de mercado e da execução do corretor.

## Diferenças vs. Script Original
- Trading contínuo sem variáveis globais manuais.
- Usa velas concluídas em vez de verificações tick a tick, o que fornece comportamento determinístico para backtests.
- Gestão de risco integrada por meio do módulo de ordens de proteção do StockSharp.

## Aviso Legal
Esta estratégia é fornecida para fins educacionais. Teste completamente em dados históricos e de demonstração antes de comprometer capital real.
