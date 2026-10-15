# Estratégia do Sistema Indicador Vortex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- **Fonte**: Convertido do consultor especializado MetaTrader 5 "Vortex Indicator System" (MQL ID 19137).
- **Conceito**: Usa o indicador Vortex para detectar cruzamentos de alta ou baixa e então arma gatilhos de rompimento no máximo/mínimo da vela do cruzamento.
- **Estilo de execução**: Seguimento de rompimento; as operações são iniciadas somente após o preço confirmar o cruzamento ao superar o nível do gatilho.
- **Regime de mercado**: Funciona em qualquer instrumento e período que suporte o indicador Vortex e dados de velas; nenhuma característica específica do corretor é necessária.
- **Tipos de ordens**: Ordens a mercado via `BuyMarket` e `SellMarket`. A estratégia fecha automaticamente posições opostas antes de enfileirar um novo gatilho.

## Lógica de trading
1. Inscrever-se no tipo de vela configurado e calcular o indicador Vortex com o comprimento especificado.
2. Detectar um cruzamento de alta quando `VI+` se move acima de `VI-` depois de estar abaixo na vela anterior:
   - Fechar qualquer posição vendida existente usando `ClosePosition()`.
   - Armazenar o máximo da vela do cruzamento como o preço de gatilho comprado.
   - Cancelar qualquer gatilho vendido pendente.
3. Detectar um cruzamento de baixa quando `VI-` se move acima de `VI+` depois de estar abaixo na vela anterior:
   - Fechar qualquer posição comprada existente.
   - Armazenar o mínimo da vela do cruzamento como o preço de gatilho vendido.
   - Cancelar qualquer gatilho comprado pendente.
4. Enquanto um gatilho está ativo, monitorar as velas subsequentes:
   - Se o preço máximo romper o gatilho comprado armazenado e a posição atual for flat ou vendida, enviar uma compra a mercado dimensionada para reverter qualquer exposição vendida.
   - Se o preço mínimo romper o gatilho vendido armazenado e a posição atual for flat ou comprada, enviar uma venda a mercado dimensionada para reverter qualquer exposição comprada.
5. Cada operação executada limpa o gatilho correspondente. Gatilhos opostos são mutuamente exclusivos.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Length` | 14 | Período do indicador Vortex. Corresponde à entrada MQL original `VI_Length`. |
| `CandleType` | Período de 60 minutos | Tipo de vela usado para o cálculo do indicador e a avaliação do gatilho. Pode ser ajustado para qualquer período suportado pela fonte de dados conectada. |
| `Volume` | Retirado da propriedade base `Strategy` | Volume de operação usado para ordens a mercado. Configure antes de iniciar a estratégia se for necessário um valor diferente de 1 contrato/lote. |

### Como os parâmetros afetam o comportamento
- Aumentar `Length` suaviza as linhas Vortex, reduzindo o número de cruzamentos, mas melhorando sua confiabilidade.
- Diminuir `Length` torna o sistema mais reativo, gerando mais gatilhos e operações potenciais.
- O `CandleType` deve ser alinhado com a granularidade de dados na configuração MQL original (tipicamente o período do gráfico). Velas mais curtas fornecem sinais mais rápidos, enquanto velas mais longas se concentram em tendências mais amplas.

## Notas de gestão de risco
- O consultor especializado original não define níveis de stop-loss ou take-profit. Esta conversão mantém esse comportamento; a gestão de risco deve ser tratada externamente ou estendendo a estratégia.
- A reversão de posição é imediata: quando ocorre um sinal oposto, a estratégia emite `ClosePosition()` e aguarda um rompimento além do gatilho antes de entrar na nova direção.
- Apenas um gatilho (comprado ou vendido) pode estar ativo de cada vez. Os gatilhos são limpos se o preço os romper ou quando ocorre um cruzamento oposto.

## Instruções de uso
1. Adicione a estratégia ao seu projeto StockSharp e certifique-se de que o pacote `StockSharp.Algo.Indicators` esteja disponível.
2. Configure o instrumento desejado e o conector na aplicação hospedeira.
3. Defina o parâmetro `CandleType` para o período que deseja operar. Deve corresponder a uma assinatura de velas disponível para o instrumento selecionado.
4. Opcionalmente ajuste `Length` e `Volume` antes de iniciar a estratégia ou através de otimização.
5. Inicie a estratégia. As ordens serão geradas assim que o indicador estiver formado e os dados em tempo real estiverem disponíveis.

## Destaques de implementação
- Usa a API de alto nível `SubscribeCandles` com vinculação de indicador (`Bind`) para processamento limpo baseado em eventos.
- Armazena os valores anteriores do Vortex para detectar cruzamentos exatamente como a implementação MQL faz (comparações de `VI+` e `VI-` entre duas velas consecutivas).
- Os gatilhos de entrada são implementados como campos decimal anuláveis para imitar o mecanismo original de "armar e romper".
- Comentários em inglês na linha do arquivo C# descrevem cada passo de decisão e ajudam a manter o código.

## Possíveis extensões
- Adicionar regras de stop-loss e take-profit (p. ex., saídas baseadas em ATR) se um controle de risco mais rígido for necessário.
- Introduzir um período de resfriamento ou tempo máximo de manutenção para evitar períodos planos prolongados quando os gatilhos não executam.
- Combinar com um filtro de volatilidade para operar apenas quando os intervalos de preço forem suficientemente amplos para justificar tentativas de rompimento.
