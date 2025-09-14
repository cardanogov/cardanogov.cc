import { GovernanceParametersResponse } from '../services/combine.service';

// Hàm băm cho một tập hợp trường cụ thể
export const hashItem = (
  item: GovernanceParametersResponse,
  fields: (keyof GovernanceParametersResponse)[]
): string => {
  const itemCopy: Partial<GovernanceParametersResponse> = {};
  fields.forEach((field) => {
    if (field in item) {
      if (field === 'cost_models') {
        // Sắp xếp các khóa trong cost_models để đảm bảo thứ tự nhất quán
        const costModels = item[field] as { [key: string]: number[] };
        const sortedCostModels: { [key: string]: number[] } = {};
        Object.keys(costModels)
          .sort()
          .forEach((key) => {
            sortedCostModels[key] = costModels[key];
          });
        itemCopy[field] = sortedCostModels;
      } else {
        itemCopy[field] = item[field];
      }
    }
  });

  const sortedStr = JSON.stringify(itemCopy);
  let hash = 0;
  for (let i = 0; i < sortedStr.length; i++) {
    hash = (hash * 31 + sortedStr.charCodeAt(i)) >>> 0;
  }
  return hash.toString();
};

// Hàm so sánh sâu cho một tập hợp trường cụ thể
export const compareItems = (
  item1: GovernanceParametersResponse,
  item2: GovernanceParametersResponse,
  fields: (keyof GovernanceParametersResponse)[]
): boolean => {
  const item1Copy: Partial<GovernanceParametersResponse> = {};
  const item2Copy: Partial<GovernanceParametersResponse> = {};

  fields.forEach((field) => {
    if (field in item1) {
      if (field === 'cost_models') {
        const costModels1 = item1[field] as { [key: string]: number[] };
        const sortedCostModels1: { [key: string]: number[] } = {};
        Object.keys(costModels1)
          .sort()
          .forEach((key) => {
            sortedCostModels1[key] = costModels1[key];
          });
        item1Copy[field] = sortedCostModels1;
      } else {
        item1Copy[field] = item1[field];
      }
    }
    if (field in item2) {
      if (field === 'cost_models') {
        const costModels2 = item2[field] as { [key: string]: number[] };
        const sortedCostModels2: { [key: string]: number[] } = {};
        Object.keys(costModels2)
          .sort()
          .forEach((key) => {
            sortedCostModels2[key] = costModels2[key];
          });
        item2Copy[field] = sortedCostModels2;
      } else {
        item2Copy[field] = item2[field];
      }
    }
  });

  return JSON.stringify(item1Copy) === JSON.stringify(item2Copy);
};

// Hàm lọc epoch cho một tập hợp trường
export const filterEpochsForFields = (
  dataList: GovernanceParametersResponse[],
  fields: (keyof GovernanceParametersResponse)[]
): number[] => {
  if (!dataList || dataList.length === 0) {
    return [];
  }

  dataList = [...dataList].sort((a, b) => (a.epoch_no || 0) - (b.epoch_no || 0));

  const groups = new Map<string, GovernanceParametersResponse[]>();
  for (const item of dataList) {
    const hash = hashItem(item, fields);
    if (!groups.has(hash)) {
      groups.set(hash, [item]);
    } else {
      const group = groups.get(hash)!;
      let added = false;
      for (const existingItem of group) {
        if (compareItems(item, existingItem, fields)) {
          group.push(item);
          added = true;
          break;
        }
      }
      if (!added) {
        groups.set(`${hash}_${groups.size}`, [item]);
      }
    }
  }

  const uniqueDataEpochs: number[] = [];
  for (const group of groups.values()) {
    const minEpochInGroup = Math.min(...group.map((item) => item.epoch_no || 0));
    uniqueDataEpochs.push(minEpochInGroup);
  }

  return uniqueDataEpochs.sort((a, b) => a - b);
};

// Hàm lọc epoch tổng hợp cho tất cả biểu đồ
export const filterEpochs = (
  dataList: GovernanceParametersResponse[],
  fieldGroups: (keyof GovernanceParametersResponse)[][],
  minItems: number = 7
): GovernanceParametersResponse[] => {
  if (!dataList || dataList.length === 0) {
    return [];
  }

  dataList = [...dataList].sort((a, b) => (a.epoch_no || 0) - (b.epoch_no || 0));

  // Lấy các epoch có thay đổi cho từng nhóm trường
  const uniqueDataEpochs = new Set<number>();
  for (const fields of fieldGroups) {
    const epochs = filterEpochsForFields(dataList, fields);
    epochs.forEach((epoch) => uniqueDataEpochs.add(epoch));
  }

  let uniqueEpochsArray = Array.from(uniqueDataEpochs).sort((a, b) => a - b);

  // Thêm epoch nhỏ nhất và lớn nhất nếu chưa có
  const minEpoch = dataList[0].epoch_no || 0;
  const maxEpoch = dataList[dataList.length - 1].epoch_no || 0;
  if (!uniqueEpochsArray.includes(minEpoch)) {
    uniqueEpochsArray.push(minEpoch);
  }
  if (!uniqueEpochsArray.includes(maxEpoch) && maxEpoch !== minEpoch) {
    uniqueEpochsArray.push(maxEpoch);
  }

  uniqueEpochsArray = uniqueEpochsArray.sort((a, b) => a - b);

  // Nếu số epoch < minItems, bổ sung thêm các epoch phân bố đều
  if (uniqueEpochsArray.length < minItems) {
    const allEpochs = dataList
      .map((item) => item.epoch_no || 0)
      .sort((a, b) => a - b);
    const numToAdd = minItems - uniqueEpochsArray.length;
    const totalRange = maxEpoch - minEpoch;
    const step = totalRange / (numToAdd + 1); // Chia khoảng cách thành numToAdd + 1 phần

    const additionalEpochs: number[] = [];
    for (let i = 1; i <= numToAdd; i++) {
      const targetEpoch = Math.round(minEpoch + step * i);
      // Tìm epoch gần nhất với targetEpoch mà chưa có trong uniqueEpochsArray
      let closestEpoch = allEpochs[0];
      let minDiff = Math.abs(allEpochs[0] - targetEpoch);
      for (const epoch of allEpochs) {
        if (
          !uniqueEpochsArray.includes(epoch) &&
          !additionalEpochs.includes(epoch)
        ) {
          const diff = Math.abs(epoch - targetEpoch);
          if (diff < minDiff) {
            minDiff = diff;
            closestEpoch = epoch;
          }
        }
      }
      if (!uniqueEpochsArray.includes(closestEpoch)) {
        additionalEpochs.push(closestEpoch);
      }
    }

    uniqueEpochsArray.push(...additionalEpochs);
    uniqueEpochsArray = uniqueEpochsArray.sort((a, b) => a - b);
  }

  // Lấy các item tương ứng
  const selectedItems: GovernanceParametersResponse[] = [];
  for (const epoch of uniqueEpochsArray) {
    const item = dataList.find((item) => item.epoch_no === epoch);
    if (item) {
      selectedItems.push(item);
    }
  }

  return selectedItems;
};
