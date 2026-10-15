# Estratégia Perceptron Mult
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista **Peceptron_Mult.mq5** para a API de alto nível do StockSharp. Ela monitora simultaneamente até três mercados independentes e aplica o oscilador Acceleration/Deceleration (AC) dentro de um modelo de perceptron. Cada mercado recebe sua própria configuração de pesos, dimensionamento de posição e saídas de proteção, de modo que o comportamento do consultor original multi-símbolo seja preservado.

## Lógica de Negociação

1. Para cada ativo configurado, a estratégia assina o mesmo tipo de vela (padrão: 1 minuto).
2. Em cada vela concluída, calcula o oscilador Acceleration/Deceleration de Bill Williams:
   - Calcular o Awesome Oscillator (AO) a partir dos máximos e mínimos da vela (médias móveis do preço mediano 5/34).
   - Subtrair uma média móvel simples de 5 períodos de AO do valor atual de AO.
3. Um buffer circular com os últimos 22 valores de AC é mantido por ativo.
4. O sinal do perceptron é formado a partir de quatro valores atrasados de AC usando pesos (`w - 100`) exatamente como no código MQL:
   - `AC[0]`, `AC[7]`, `AC[14]`, `AC[21]` correspondem à leitura mais recente e três históricas.
5. Regras de entrada:
   - Soma positiva ⇒ abrir posição comprada se nenhuma posição existir naquele ativo.
   - Soma negativa ⇒ abrir posição vendida se o ativo estiver plano.
6. Regras de saída:
   - As distâncias de stop-loss e take-profit são expressas em pontos. São convertidas em deslocamentos de preço absolutos usando o passo de preço do instrumento.
   - As saídas de proteção são avaliadas em cada vela concluída. Uma negociação comprada é fechada quando a mínima da vela atinge o stop ou a máxima alcança o alvo de lucro; vendidas usam a lógica espelhada.
7. As posições são mutuamente exclusivas por ativo. A estratégia ignora novos sinais enquanto a exposição permanece aberta, replicando o comportamento do consultor original.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `FirstSecurity`, `SecondSecurity`, `ThirdSecurity` | Instrumentos processados pelo perceptron. Deixar em `null` para desativar um slot.
| `FirstOrderVolume`, `SecondOrderVolume`, `ThirdOrderVolume` | Tamanho da ordem a mercado para cada instrumento.
| `FirstWeight1`…`FirstWeight4`, etc. | Pesos do perceptron (entradas MQL `x1…x12`). A estratégia subtrai internamente 100 de cada valor antes de aplicá-lo.
| `FirstStopLossPoints`, `SecondStopLossPoints`, `ThirdStopLossPoints` | Distância do stop-loss em pontos de preço para cada instrumento. Definir como 0 para desativar.
| `FirstTakeProfitPoints`, `SecondTakeProfitPoints`, `ThirdTakeProfitPoints` | Distância do take-profit em pontos de preço para cada instrumento. Definir como 0 para desativar.
| `CandleType` | Série de velas compartilhada por todos os ativos.

## Notas de Implementação

- A estratégia depende dos indicadores `AwesomeOscillator` e `SimpleMovingAverage` do StockSharp para reconstruir o oscilador AC, evitando recálculos manuais.
- Os buffers circulares são usados apenas para emular as entradas do perceptron da implementação MQL (índices 0, 7, 14, 21).
- Os níveis de proteção são aplicados sem registrar ordens stop separadas: a estratégia monitora os extremos das velas e fecha posições com ordens a mercado quando os níveis são violados, refletindo o comportamento do EA original em novos ticks.
- Cada ativo mantém estado de indicador independente, volume de ordem e configurações de risco, correspondendo à estrutura de três símbolos do consultor de origem.

## Dicas de Uso

1. Atribuir até três ativos no painel de parâmetros. Qualquer slot não utilizado pode permanecer como `null`.
2. Ajustar os stops e alvos baseados em pontos para corresponder ao tamanho de tick dos instrumentos selecionados.
3. Ajustar os pesos do perceptron para enfatizar atrasos específicos do oscilador AC se a otimização for necessária.
4. Como todos os instrumentos compartilham o mesmo tipo de vela, garantir que dados históricos estejam disponíveis para cada ativo configurado.
