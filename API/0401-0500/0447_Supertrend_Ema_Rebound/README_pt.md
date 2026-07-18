# Estratégia SuperTrend + EMA Rebound
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O sistema opera na direção do SuperTrend e procura pullbacks para uma média móvel
exponencial. Uma posição é aberta quando a linha SuperTrend muda de direção ou quando
o preço rebate da EMA enquanto permanece no viés SuperTrend prevalente. Esta combinação
tenta capturar a primeira perna de um novo movimento e as subsequentes correções dentro
de uma tendência estabelecida.

Um take profit baseado em porcentagem pode ser habilitado via módulo de proteção
integrado configurando o tipo de take profit como "%". Os padrões favorecem operações
compradas, mas entradas vendidas também podem ser ativadas. Como a estratégia depende
de mudanças de direção, ela é mais eficaz em mercados em tendência onde o SuperTrend
reage rapidamente às mudanças de momentum.

## Detalhes

- **Critérios de entrada**:
  - SuperTrend muda para tendência de alta, ou preço rebate acima da EMA durante tendência de alta.
  - SuperTrend muda para tendência de baixa, ou preço rebate abaixo da EMA durante tendência de baixa.
- **Comprado/Vendido**: Comprado habilitado por padrão, vendido opcional.
- **Critérios de saída**:
  - Inversão oposta do SuperTrend.
  - Take profit opcional gerenciado pelo módulo de proteção.
- **Stops**: Take profit percentual via proteção; sem stop loss incluído.
- **Valores padrão**:
  - Período ATR = 10, fator ATR = 3.0.
  - Comprimento EMA = 20, TP = 1.5%.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos (comprado por padrão)
  - Indicadores: SuperTrend, EMA
  - Stops: TP opcional
  - Complexidade: Moderado
  - Período: Curto/médio
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
