# Estratégia Bull vs Medved
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Bull vs Medved é uma estratégia de rompimento com ordens limite originalmente publicada para MetaTrader 5. Ela observa as últimas três velas completadas e permite trades apenas durante seis janelas de cinco minutos distribuídas uniformemente ao longo do dia. Quando aparece uma sequência específica de velas altistas ou baixistas, a estratégia coloca uma ordem limite pendente deslocada do spread atual e protege a posição com alvos simétricos de stop-loss e take-profit.

## Lógica de Trading

1. **Janelas de trading** – ordens são avaliadas apenas se a hora do dia atual estiver dentro de uma das seis janelas configuráveis (padrão 00:05, 04:05, 08:05, 12:05, 16:05, 20:05) e dentro da duração configurada (5 minutos por padrão). Sair da janela redefine o guard de uma ordem por janela.
2. **Dados de velas necessários** – a estratégia aguarda três velas concluídas antes de gerar sinais. Os cálculos sempre usam as três velas mais recentes completadas.
3. **Setups altistas**:
   - **Bull regular**: a vela de três períodos atrás fecha acima da abertura da segunda vela, a segunda vela tem pelo menos um corpo altista de 1 pip, e a vela mais recente tem um corpo altista maior que o limite `CandleSizePips` configurado.
   - **Filtro bad bull**: se as três velas têm grandes corpos altistas, o sinal é ignorado para evitar movimentos parabólicos.
   - **Cool bull**: após um forte recuo baixista (segunda vela fecha pelo menos 2 pips abaixo de sua abertura), a vela mais recente deve envolver o recuo e imprimir pelo menos 40% do corpo normal `CandleSizePips`. Um bull regular (sem o filtro bad-bull) ou um padrão cool bull aciona um setup comprado.
   - Com um sinal comprado válido, a estratégia coloca uma ordem **buy limit** abaixo do melhor ask em `IndentUpPips` (convertido para unidades de preço do instrumento).
4. **Setup baixista**:
   - Se a vela mais recente tem um corpo baixista maior que `CandleSizePips`, a estratégia coloca uma ordem **sell limit** acima do melhor bid em `IndentDownPips`.
5. **Gerenciamento de risco** – uma vez que uma posição é aberta, a estratégia anexa automaticamente alvos absolutos de stop-loss e take-profit usando as distâncias em pips configuradas.
6. **Gerenciamento de ordens** – apenas uma ordem pode ser enviada por janela e nenhuma nova ordem é colocada enquanto outra ordem limite para o mesmo símbolo permaneça ativa.

## Parâmetros

- `OrderVolume` – volume de trading para ordens limite.
- `CandleSizePips` – tamanho mínimo do corpo altista/baixista para a última vela.
- `StopLossPips` – distância do stop de proteção a partir do preço de entrada.
- `TakeProfitPips` – distância do alvo de lucro a partir do preço de entrada.
- `IndentUpPips` – deslocamento do buy limit abaixo do melhor ask.
- `IndentDownPips` – deslocamento do sell limit acima do melhor bid.
- `EntryWindowMinutes` – duração de cada janela de trading permitida.
- `CandleType` – período de velas usado para avaliar padrões.
- `StartTime0` … `StartTime5` – horários de início das seis janelas de trading.

## Notas Adicionais

- A estratégia assina o livro de ordens para manter os preços bid/ask mais recentes para colocação precisa de limites. Se não houver dados do livro disponíveis, recorre ao último fechamento de vela.
- Os offsets de preço são calculados em unidades de tamanho pip que se adaptam automaticamente a cotações de 3 e 5 dígitos.
- O stop-loss e take-profit são aplicados através de `StartProtection` para que os alvos sigam o preço de execução real da ordem limite.
