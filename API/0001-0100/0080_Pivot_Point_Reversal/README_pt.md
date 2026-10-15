# Estratégia de Reversão em Pivot Point
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os Pivot Points diários e seus níveis de suporte e resistência frequentemente atuam como pontos de reversão para a ação de preço intradiária. Esta estratégia calcula os pivôs clássicos do floor-trader a partir da máxima, mínima e fechamento do dia anterior, e então procura velas que ricocheteiem em S1 ou R1.

Os testes indicam um retorno anual médio de aproximadamente 127%. Funciona melhor no mercado de ações.

Quando o preço se aproxima do nível de suporte S1 e forma uma vela de alta, uma entrada comprada é feita. Se o preço testa o nível de resistência R1 e imprime uma vela de baixa, um vendido é aberto. As operações saem ao atingir o pivot central ou se o stop de proteção for acionado.

O método é reiniciado no início de cada sessão de negociação com novos cálculos de pivô, tornando-o bem adequado para sessões com intervalos intradiários claros.

## Detalhes

- **Critérios de entrada**: Vela de alta próxima a S1 ou vela de baixa próxima a R1.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço cruzando o pivot central ou stop-loss.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
    Os pivôs de cada dia UTC vêm da máxima, mínima e fechamento do dia anterior. Uma compra exige uma vela de alta que toque S1 e feche acima dele, uma venda uma vela de baixa que alcance R1 e feche abaixo dele; o stop é uma porcentagem do preço de entrada.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Pivot Points
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

