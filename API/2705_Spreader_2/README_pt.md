# Estratégia Spreader 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Spreader 2** é um sistema de trading de pares convertido do consultor especialista de MetaTrader "Spreader 2". Ela observa dois instrumentos correlacionados em um período de um minuto e busca desvios de curto prazo entre seus movimentos de preço. Quando ambas as pernas divergem dentro de limites de volatilidade controlada mantendo correlação positiva, a estratégia abre um spread de mercado neutro indo comprado em um símbolo e vendido no outro. A posição combinada é fechada quando o lucro flutuante total atinge o alvo configurado ou quando as regras de correlação são violadas.

## Lógica principal

1. Receber candles finalizados para os símbolos primário e secundário e alinhá-los por tempo de fechamento.
2. Manter listas contínuas de preços de fechamento para que o algoritmo possa referenciar valores que estão `ShiftLength`, `2 * ShiftLength` e `1440` barras no passado.
3. Calcular primeiras diferenças (`x1`, `x2` para o símbolo primário e `y1`, `y2` para o símbolo secundário) para detectar oscilações locais.
4. Pular negociações quando qualquer instrumento mostra dois movimentos consecutivos na mesma direção (filtro de tendência) ou quando os produtos `x1 * y1` indicam correlação negativa.
5. Avaliar a razão de volatilidade `a / b` onde `a = |x1| + |x2|` e `b = |y1| + |y2|`. Prosseguir apenas quando a razão permanece entre `0.3` e `3.0`.
6. Escalar o volume da perna secundária proporcionalmente à razão de volatilidade e ajustá-lo ao passo de volume do contrato, mínimo e máximo.
7. Confirmar a direção de negociação pretendida com o histórico de 1440 barras (aproximadamente um dia de negociação). O spread só é aberto quando o movimento diário apoia o sinal de curto prazo.
8. A estratégia abre ambas as pernas simultaneamente: o símbolo primário negocia com o `PrimaryVolume` configurado, enquanto o símbolo secundário negocia o tamanho ajustado na direção oposta.
9. Enquanto as posições estão abertas, o sistema rastreia continuamente o lucro flutuante de ambas as pernas. Quando o lucro combinado excede `TargetProfit`, ele fecha o spread e redefine as referências de entrada.
10. Verificações de segurança fecham automaticamente posições órfãs se uma perna sair inesperadamente e reabre as pernas ausentes quando possível para manter o hedge equilibrado.

## Parâmetros

- **SecondSecurity** – instrumento secundário participante do spread. Este parâmetro é obrigatório.
- **PrimaryVolume** – volume de negociação (em lotes/contratos) para o símbolo primário. O padrão é `1`.
- **TargetProfit** – alvo de lucro monetário absoluto para o par combinado. O padrão é `100`.
- **ShiftLength** – número de candles entre os pontos de comparação usados nos cálculos de primeira diferença. O padrão é `30`.
- **CandleType** – tipo de dado usado para assinaturas de candles. Por padrão a estratégia trabalha com candles de um minuto.

## Regras de negociação

- Apenas candles finalizados são processados para evitar ações em dados incompletos.
- Os filtros de tendência devem mostrar movimentos opostos nas últimas duas janelas de `ShiftLength` para ambos os símbolos.
- A correlação deve ser positiva, e a razão de volatilidade deve permanecer na banda `[0.3, 3.0]`.
- A verificação de confirmação contra o histórico de 1440 barras previne negociações que contradizem a direção de longo prazo.
- As ordens são enviadas com `OrderTypes.Market`. A perna secundária é registrada explicitamente com o instrumento secundário e a carteira para espelhar o comportamento do MetaTrader.
- O lucro aberto é calculado usando os últimos fechamentos de candles e os preços de entrada armazenados para determinar quando sair do spread.

## Notas

- A estratégia assume que ambos os instrumentos compartilham especificações de contrato compatíveis. Se os multiplicadores diferem, o trading é desabilitado e um aviso é registrado.
- Como o algoritmo original depende de um dia completo de dados históricos, a versão StockSharp também espera até que pelo menos 1440 candles sejam acumulados antes da primeira entrada.
- Toda a lógica de gestão de risco (alvo de lucro, tratamento de perna órfã) está contida na estratégia. Proteções adicionais como stop-losses podem ser adicionadas externamente se necessário.
