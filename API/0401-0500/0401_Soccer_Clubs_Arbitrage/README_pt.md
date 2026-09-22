# Estratégia de Arbitragem de Clubes de Futebol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compara os fechamentos de velas concluídas de dois instrumentos relacionados. Ela calcula o prêmio relativo como `principal / segundo - 1` e negocia as duas pernas quando o prêmio absoluto supera o limite de entrada.

Se o instrumento principal estiver mais caro, a estratégia o vende e compra o segundo com o mesmo volume em unidades. Se o segundo estiver mais caro, as direções são invertidas. As duas posições são encerradas quando o prêmio absoluto cai abaixo do limite de saída.

## Detalhes

- **Dados**: Velas concluídas do ativo principal e de `Security2Id`; o período padrão é de cinco minutos.
- **Entrada**: Ordens a mercado opostas com o mesmo número de unidades quando o prêmio supera `EntryThreshold` em qualquer direção.
- **Saída**: Encerramento da posição real de cada perna quando o prêmio absoluto fica abaixo de `ExitThreshold`.
- **Pausa**: Após entrada, saída ou reversão, aguarda `CooldownBars` atualizações pareadas de velas.
- **Risco de execução**: As duas ordens a mercado são enviadas separadamente e não são atômicas. Quantidades iguais também não garantem nocionais iguais; permanecem os riscos de execução de uma única perna, liquidez e tamanho do contrato.

